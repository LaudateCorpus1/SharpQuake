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

using System.Globalization;
using OpenTK;

namespace SharpQuake
{
    internal static partial class QCommon
    {
        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static           v3f    ZeroVector3f = default( v3f );
        private static readonly byte[] ZeroBytes    = new byte[4096];


        public static short BigShort( short l )
        {
            return _Converter.BigShort( l );
        }

        public static short LittleShort( short l )
        {
            return _Converter.LittleShort( l );
        }

        public static int BigLong( int l )
        {
            return _Converter.BigLong( l );
        }

        public static int LittleLong( int l )
        {
            return _Converter.LittleLong( l );
        }

        public static float BigFloat( float l )
        {
            return _Converter.BigFloat( l );
        }

        public static float LittleFloat( float l )
        {
            return _Converter.LittleFloat( l );
        }

        public static Vector3 LittleVector( Vector3 src )
        {
            return new Vector3( _Converter.LittleFloat( src.X ),
                                _Converter.LittleFloat( src.Y ), _Converter.LittleFloat( src.Z ) );
        }

        public static Vector3 LittleVector3( float[] src )
        {
            return new Vector3( _Converter.LittleFloat( src[0] ),
                                _Converter.LittleFloat( src[1] ), _Converter.LittleFloat( src[2] ) );
        }

        public static Vector4 LittleVector4( float[] src, int offset )
        {
            return new Vector4( _Converter.LittleFloat( src[offset + 0] ),
                                _Converter.LittleFloat( src[offset + 1] ),
                                _Converter.LittleFloat( src[offset + 2] ),
                                _Converter.LittleFloat( src[offset + 3] ) );
        }

        public static void Copy( float[] src, out Vector3 dest )
        {
            dest.X = src[0];
            dest.Y = src[1];
            dest.Z = src[2];
        }

        public static void Copy( ref Vector3 src, float[] dest )
        {
            dest[0] = src.X;
            dest[1] = src.Y;
            dest[2] = src.Z;
        }

        public static Vector3 ToVector( ref v3f v )
        {
            return new Vector3( v.x, v.y, v.z );
        }

        public static void WriteInt( byte[] dest, int offset, int value )
        {
            QByteUnion4 u = QByteUnion4.Empty;
            u.i0             = value;
            dest[offset + 0] = u.b0;
            dest[offset + 1] = u.b1;
            dest[offset + 2] = u.b2;
            dest[offset + 3] = u.b3;
        }

        public static int atoi( string s )
        {
            if( string.IsNullOrEmpty( s ) )
                return 0;

            int sign   = 1;
            int result = 0;
            int offset = 0;
            if( s.StartsWith( "-" ) )
            {
                sign = -1;
                offset++;
            }

            int i = -1;

            if( s.Length > 2 )
            {
                i = s.IndexOf( "0x", offset, 2 );
                if( i == -1 )
                {
                    i = s.IndexOf( "0X", offset, 2 );
                }
            }

            if( i == offset )
            {
                int.TryParse( s.Substring( offset + 2 ), System.Globalization.NumberStyles.HexNumber, null, out result );
            }
            else
            {
                i = s.IndexOf( '\'', offset, 1 );
                if( i != -1 )
                {
                    result = (byte) s[i + 1];
                }
                else
                    int.TryParse( s.Substring( offset ), out result );
            }

            return sign * result;
        }

        public static float atof( string s )
        {
            float.TryParse( s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out float v );
            return v;
        }
    }
}
