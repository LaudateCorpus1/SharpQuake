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
using NVorbis.OpenTKSupport;

namespace SharpQuake
{
    internal class QNullCDAudioController
    {
        private byte[]    _Remap;
        private OggStream oggStream;

        private OggStreamer streamer;

        //private WaveOutEvent waveOut; // or WaveOutEvent()
        private bool  _isLooping;
        string        trackid;
        string        trackpath;
        private bool  _noAudio    = false;
        private bool  _noPlayback = false;
        private float _Volume;
        private bool  _isPlaying;
        private bool  _isPaused;


        //OGG file
        int      channels;
        int      sampleRate;
        TimeSpan totalTime;

        public QNullCDAudioController()
        {
            _Remap = new byte[100];
        }

#region ICDAudioController Members

        public bool IsInitialized { get { return true; } }

        public bool IsEnabled { get { return true; } set {} }

        public bool IsPlaying { get { return _isPlaying; } }

        public bool IsPaused { get { return _isPaused; } }

        public bool IsValidCD { get { return false; } }

        public bool IsLooping { get { return _isLooping; } }

        public byte[] Remap { get { return _Remap; } }

        public byte MaxTrack { get { return 0; } }

        public byte CurrentTrack { get { return 0; } }

        public float Volume { get { return _Volume; } set { _Volume = value; } }

        public void Init()
        {
            streamer = new OggStreamer( 441000 );
            _Volume  = QSound.BgmVolume;

            if( Directory.Exists( string.Format( "{0}/{1}/music/", qparam.globalbasedir, qparam.globalgameid ) ) == false )
            {
                _noAudio = true;
            }
        }

        public void Play( byte track, bool looping )
        {
            if( _noAudio == false )
            {
                trackid   = track.ToString( "00" );
                trackpath = string.Format( "{0}/{1}/music/track{2}.ogg", qparam.globalbasedir, qparam.globalgameid, trackid );
#if DEBUG
                Console.WriteLine( "DEBUG: track path:{0} ", trackpath );
#endif
                try
                {
                    _isLooping = looping;
                    if( oggStream != null )
                        oggStream.Stop();
                    oggStream          = new OggStream( trackpath, 3 );
                    oggStream.IsLooped = looping;
                    oggStream.Play();
                    oggStream.Volume = _Volume;
                    _noPlayback      = false;
                }
                catch( Exception e )
                {
                    Console.WriteLine( "Could not find or play {0}", trackpath );
                    _noPlayback = true;
                    //throw;
                }
            }
        }

        public void Stop()
        {
            if( streamer == null )
                return;

            if( _noAudio == true )
                return;

            oggStream.Stop();
        }

        public void Pause()
        {
            if( streamer == null )
                return;

            if( _noAudio == true )
                return;

            oggStream.Pause();
        }

        public void Resume()
        {
            if( streamer == null )
                return;

            oggStream.Resume();
        }

        public void Shutdown()
        {
            if( streamer == null )
                return;

            if( _noAudio == true )
                return;

            //oggStream.Dispose();
            streamer.Dispose();
        }

        public void Update()
        {
            if( streamer == null )
                return;

            if( _noAudio == true )
                return;

            if( _noPlayback == true )
                return;

            /*if (waveOut.PlaybackState == PlaybackState.Paused)
            {
                _isPaused = true;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPaused = false;
            }

            if (waveOut.PlaybackState == PlaybackState.Paused || waveOut.PlaybackState == PlaybackState.Stopped)
            {
                _isPlaying = false;
            }
            else if (waveOut.PlaybackState == PlaybackState.Playing)
            {
                _isPlaying = true;
            }*/

            _Volume          = QSound.BgmVolume;
            oggStream.Volume = _Volume;
        }

        public void ReloadDiskInfo()
        {
        }

        public void CloseDoor()
        {
        }

        public void Eject()
        {
        }

#endregion ICDAudioController Members
    }
}
