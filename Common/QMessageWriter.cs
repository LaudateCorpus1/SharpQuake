/* Rewritten in C# by Yury Kiselev, 2010.
 *
 * Copyright (C) 1996-1997 Id Software, Inc.
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *
 * See the GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace SharpQuake
{
    // MSG_WriteXxx() functions
    internal class QMessageWriter
    {
        public byte[] Data    => _Buffer;
        public bool   IsEmpty => ( _Count == 0 );
        public int    Length  => _Count;

        public bool AllowOverflow { get; set; }
        public bool IsOveflowed   { get; set; }

        public int Capacity { get => _Buffer.Length; set => SetBufferSize( value ); }

        private byte[] _Buffer;

        private int _Count;

        private QByteUnion4 _Val = QByteUnion4.Empty;

        public object GetState()
        {
            object st = null;
            SaveState( ref st );
            return st;
        }

        public void SaveState( ref object state )
        {
            if( state == null )
            {
                state = new State();
            }

            State st = GetState( state );
            if( st.Buffer == null || st.Buffer.Length != _Buffer.Length )
            {
                st.Buffer = new byte[_Buffer.Length];
            }

            Buffer.BlockCopy( _Buffer, 0, st.Buffer, 0, _Buffer.Length );
            st.Count = _Count;
        }

        public void RestoreState( object state )
        {
            State st = GetState( state );
            SetBufferSize( st.Buffer.Length );
            Buffer.BlockCopy( st.Buffer, 0, _Buffer, 0, _Buffer.Length );
            _Count = st.Count;
        }

        // void MSG_WriteChar(sizebuf_t* sb, int c);
        public void WriteChar( int c )
        {
#if PARANOID
            if (c < -128 || c > 127)
                Sys.Error("MSG_WriteChar: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = (byte) c;
        }

        // MSG_WriteByte(sizebuf_t* sb, int c);
        public void WriteByte( int c )
        {
#if PARANOID
            if (c < 0 || c > 255)
                Sys.Error("MSG_WriteByte: range error");
#endif
            NeedRoom( 1 );
            _Buffer[_Count++] = (byte) c;
        }

        // MSG_WriteShort(sizebuf_t* sb, int c)
        public void WriteShort( int c )
        {
#if PARANOID
            if (c < short.MinValue || c > short.MaxValue)
                Sys.Error("MSG_WriteShort: range error");
#endif
            NeedRoom( 2 );
            _Buffer[_Count++] = (byte) ( c & 0xff );
            _Buffer[_Count++] = (byte) ( c >> 8 );
        }

        // MSG_WriteLong(sizebuf_t* sb, int c);
        public void WriteLong( int c )
        {
            NeedRoom( 4 );
            _Buffer[_Count++] = (byte) ( c           & 0xff );
            _Buffer[_Count++] = (byte) ( ( c >> 8 )  & 0xff );
            _Buffer[_Count++] = (byte) ( ( c >> 16 ) & 0xff );
            _Buffer[_Count++] = (byte) ( c >> 24 );
        }

        // MSG_WriteFloat(sizebuf_t* sb, float f)
        public void WriteFloat( float f )
        {
            NeedRoom( 4 );
            _Val.f0 = f;
            _Val.i0 = QCommon.LittleLong( _Val.i0 );

            _Buffer[_Count++] = _Val.b0;
            _Buffer[_Count++] = _Val.b1;
            _Buffer[_Count++] = _Val.b2;
            _Buffer[_Count++] = _Val.b3;
        }

        // MSG_WriteString(sizebuf_t* sb, char* s)
        public void WriteString( string s )
        {
            int count = 1;
            if( !string.IsNullOrEmpty( s ) )
                count += s.Length;

            NeedRoom( count );
            for( int i = 0; i < count - 1; i++ )
                _Buffer[_Count++] = (byte) s[i];
            _Buffer[_Count++] = 0;
        }

        // SZ_Print()
        public void Print( string s )
        {
            if( _Count > 0 && _Buffer[_Count - 1] == 0 )
                _Count--; // remove previous trailing 0
            WriteString( s );
        }

        // MSG_WriteCoord(sizebuf_t* sb, float f)
        public void WriteCoord( float f )
        {
            WriteShort( (int) ( f * 8 ) );
        }

        // MSG_WriteAngle(sizebuf_t* sb, float f)
        public void WriteAngle( float f )
        {
            WriteByte( ( (int) f * 256 / 360 ) & 255 );
        }

        public void Write( byte[] src, int offset, int count )
        {
            if( count > 0 )
            {
                NeedRoom( count );
                Buffer.BlockCopy( src, offset, _Buffer, _Count, count );
                _Count += count;
            }
        }

        public void Clear()
        {
            _Count = 0;
        }

        public void FillFrom( Stream src, int count )
        {
            Clear();
            NeedRoom( count );
            while( _Count < count )
            {
                int r = src.Read( _Buffer, _Count, count - _Count );
                if( r == 0 )
                    break;
                _Count += r;
            }
        }

        public void FillFrom( byte[] src, int startIndex, int count )
        {
            Clear();
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, 0, count );
            _Count = count;
        }

        public int FillFrom( Socket socket, ref EndPoint ep )
        {
            Clear();
            int result = net.LanDriver.Read( socket, _Buffer, _Buffer.Length, ref ep );
            if( result >= 0 )
                _Count = result;
            return result;
        }

        public void AppendFrom( byte[] src, int startIndex, int count )
        {
            NeedRoom( count );
            Buffer.BlockCopy( src, startIndex, _Buffer, _Count, count );
            _Count += count;
        }

        protected void NeedRoom( int bytes )
        {
            if( _Count + bytes > _Buffer.Length )
            {
                if( !this.AllowOverflow )
                    sys.Error( "QMessageWriter: overflow without allowoverflow set!" );

                this.IsOveflowed = true;
                _Count           = 0;
                if( bytes > _Buffer.Length )
                    sys.Error( "QMessageWriter: Requested more than whole buffer has!" );
            }
        }

        private class State
        {
            public byte[] Buffer;
            public int    Count;
        }

        private void SetBufferSize( int value )
        {
            if( _Buffer != null )
            {
                if( _Buffer.Length == value )
                    return;

                Array.Resize( ref _Buffer, value );

                if( _Count > _Buffer.Length )
                    _Count = _Buffer.Length;
            }
            else
                _Buffer = new byte[value];
        }

        private State GetState( object state )
        {
            if( state == null )
            {
                throw new ArgumentNullException();
            }

            State st = state as State;
            if( st == null )
            {
                throw new ArgumentException( "Passed object is not a state!" );
            }

            return st;
        }

        public QMessageWriter()
            : this( 0 )
        {
        }

        public QMessageWriter( int capacity )
        {
            SetBufferSize( capacity );
            this.AllowOverflow = false;
        }
    }
}
