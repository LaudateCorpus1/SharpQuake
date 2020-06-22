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

using System.Text;

namespace SharpQuake
{
    internal class QSoundWAVHelper
    {
        private byte[] _Wav;

        public int FindChunk( string name, int startFromChunk )
        {
            int offset    = startFromChunk;
            int lastChunk = offset;
            while( true )
            {
                offset = lastChunk;         //data_p = last_chunk;
                if( offset >= _Wav.Length ) // data_p >= iff_end)
                    break;                  // didn't find the chunk

                //offset += 4; // data_p += 4;
                int iff_chunk_len = GetLittleLong( offset + 4 );
                if( iff_chunk_len < 0 )
                    break;

                //data_p -= 8;
                lastChunk = offset + 8 + ( ( iff_chunk_len + 1 ) & ~1 );
                //last_chunk = data_p + 8 + ((iff_chunk_len + 1) & ~1);
                string chunkName = Encoding.ASCII.GetString( _Wav, offset, 4 );
                if( chunkName == name )
                    return offset;
            }

            return -1;
        }

        public short GetLittleShort( int index )
        {
            return (short) ( _Wav[index] + (short) ( _Wav[index + 1] << 8 ) );
        }

        public int GetLittleLong( int index )
        {
            return _Wav[index] + ( _Wav[index + 1] << 8 ) + ( _Wav[index + 2] << 16 ) + ( _Wav[index + 3] << 24 );
        }

        public QSoundWAVHelper( byte[] wav )
        {
            _Wav = wav;
        }
    }
}
