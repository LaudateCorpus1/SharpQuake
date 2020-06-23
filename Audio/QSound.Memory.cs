/// <copyright>
///
/// Rewritten in C# by Yury Kiselev, 2010.
///
/// Copyright (C) 1996-1997 Id Software, Inc.
///
/// This program is free software; you can redistribute it and/or
/// modify it under the terms of the GNU General Public License
/// as published by the Free Software Foundation; either version 2
/// of the License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
///
/// See the GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program; if not, write to the Free Software
/// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
/// </copyright>

using System;
using System.Text;

// snd_mem.c

namespace SharpQuake
{
    partial class QSound
    {
        // GetWavinfo
        private static QSoundWAVInfo GetWavInfo( string name, byte[] wav )
        {
            QSoundWAVInfo info = new QSoundWAVInfo();

            if( wav == null )
                return info;

            // debug
            //using (FileStream fs = new FileStream(Path.GetFileName(name), FileMode.Create, FileAccess.Write, FileShare.Read))
            //{
            //    fs.Write(wav, 0, wav.Length);
            //}
            QSoundWAVHelper helper = new QSoundWAVHelper( wav );

            int offset = 0;

            // find "RIFF" chunk
            int riff = helper.FindChunk( "RIFF", offset );
            if( riff == -1 )
            {
                Con.Print( "Missing RIFF chunk\n" );
                return info;
            }

            string wave = Encoding.ASCII.GetString( wav, offset + 8, 4 );
            if( wave != "WAVE" )
            {
                Con.Print( "RIFF chunk is not WAVE\n" );
                return info;
            }

            // get "fmt " chunk
            offset += 12; //iff_data = data_p + 12;

            int fmt = helper.FindChunk( "fmt ", offset );
            if( fmt == -1 )
            {
                Con.Print( "Missing fmt chunk\n" );
                return info;
            }

            int format = helper.GetLittleShort( fmt + 8 );
            if( format != 1 )
            {
                Con.Print( "Microsoft PCM format only\n" );
                return info;
            }

            info.channels = helper.GetLittleShort( fmt + 10 );
            info.rate     = helper.GetLittleLong( fmt  + 12 );
            info.width    = helper.GetLittleShort( fmt + 16 + 4 + 2 ) / 8;

            // get cue chunk
            int cue = helper.FindChunk( "cue ", offset );
            if( cue != -1 )
            {
                info.loopstart = helper.GetLittleLong( cue + 32 );

                // if the next chunk is a LIST chunk, look for a cue length marker
                int list = helper.FindChunk( "LIST", cue );
                if( list != -1 )
                {
                    string mark = Encoding.ASCII.GetString( wav, list + 28, 4 );
                    if( mark == "mark" )
                    {
                        // this is not a proper parse, but it works with cooledit...
                        int i = helper.GetLittleLong( list + 24 ); // samples in loop
                        info.samples = info.loopstart + i;
                    }
                }
            }
            else
                info.loopstart = -1;

            // find data chunk
            int data = helper.FindChunk( "data", offset );
            if( data == -1 )
            {
                Con.Print( "Missing data chunk\n" );
                return info;
            }

            int samples = helper.GetLittleLong( data + 4 ) / info.width;
            if( info.samples > 0 )
            {
                if( samples < info.samples )
                    sys.Error( "Sound {0} has a bad loop length", name );
            }
            else
                info.samples = samples;

            info.dataofs = data + 8;

            return info;
        }

        // ResampleSfx
        private static void ResampleSfx( QSoundFX sfx, int inrate, int inwidth, QByteArraySegment data )
        {
            QSoundFXCache sc = (QSoundFXCache) Cache.Check( sfx.cache );
            if( sc == null )
                return;

            float stepscale = (float) inrate / _shm.speed; // this is usually 0.5, 1, or 2

            int outcount = (int) ( sc.length / stepscale );
            sc.length = outcount;
            if( sc.loopstart != -1 )
                sc.loopstart = (int) ( sc.loopstart / stepscale );

            sc.speed = _shm.speed;
            if( _LoadAs8bit.Value != 0 )
                sc.width = 1;
            else
                sc.width = inwidth;
            sc.stereo = 0;

            sc.data = new byte[outcount * sc.width]; // uze: check this later!!!

            // resample / decimate to the current source rate
            byte[] src = data.Data;
            if( stepscale == 1 && inwidth == 1 && sc.width == 1 )
            {
                // fast special case
                for( int i = 0; i < outcount; i++ )
                {
                    int v = src[data.StartIndex + i] - 128;
                    sc.data[i] = (byte) ( (sbyte) v ); //((signed char *)sc.data)[i] = (int)( (unsigned char)(data[i]) - 128);
                }
            }
            else
            {
                // general case
                int     samplefrac = 0;
                int     fracstep   = (int) ( stepscale * 256 );
                int     sample;
                short[] sa = new short[1];
                for( int i = 0; i < outcount; i++ )
                {
                    int srcsample = samplefrac >> 8;
                    samplefrac += fracstep;
                    if( inwidth == 2 )
                    {
                        Buffer.BlockCopy( src, data.StartIndex + srcsample * 2, sa, 0, 2 );
                        sample = QCommon.LittleShort( sa[0] ); //  ((short *)data)[srcsample] );
                    }
                    else
                    {
                        sample = (int) ( src[data.StartIndex + srcsample] - 128 ) << 8;
                        //sample = (int)( (unsigned char)(data[srcsample]) - 128) << 8;
                    }

                    if( sc.width == 2 )
                    {
                        sa[0] = (short) sample;
                        Buffer.BlockCopy( sa, 0, sc.data, i * 2, 2 ); //((short *)sc->data)[i] = sample;
                    }
                    else
                    {
                        sc.data[i] = (byte) (sbyte) ( sample >> 8 ); //((signed char *)sc->data)[i] = sample >> 8;
                    }
                }
            }
        }
    }
}
