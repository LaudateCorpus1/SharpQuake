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

namespace SharpQuake
{
    /// <summary>
    /// CDAudio_functions
    /// </summary>
    internal static class QCDAudio
    {
        static QNullCDAudioController _Controller = new QNullCDAudioController();

        /// <summary>
        /// CDAudio_Init
        /// </summary>
        public static bool Init()
        {
            if( QClient.cls.state == ServerType.DEDICATED )
                return false;

            if( QCommon.HasParam( "-nocdaudio" ) )
                return false;

            _Controller.Init();

            if( _Controller.IsInitialized )
            {
                QCommand.Add( "cd", CD_f );
                Con.Print( "CD Audio (Fallback) Initialized\n" );
            }

            return _Controller.IsInitialized;
        }

        // CDAudio_Play(byte track, qboolean looping)
        public static void Play( byte track, bool looping )
        {
            _Controller.Play( track, looping );
#if DEBUG
            Console.WriteLine( "DEBUG: track byte:{0} - loop byte: {1}", track, looping );
#endif
        }

        // CDAudio_Stop
        public static void Stop()
        {
            _Controller.Stop();
        }

        // CDAudio_Pause
        public static void Pause()
        {
            _Controller.Pause();
        }

        // CDAudio_Resume
        public static void Resume()
        {
            _Controller.Resume();
        }

        // CDAudio_Shutdown
        public static void Shutdown()
        {
            _Controller.Shutdown();
        }

        // CDAudio_Update
        public static void Update()
        {
            _Controller.Update();
        }

        private static void CD_f()
        {
            if( QCommand.Argc < 2 )
                return;

            string command = QCommand.Argv( 1 );

            if( QCommon.SameText( command, "on" ) )
            {
                _Controller.IsEnabled = true;
                return;
            }

            if( QCommon.SameText( command, "off" ) )
            {
                if( _Controller.IsPlaying )
                    _Controller.Stop();
                _Controller.IsEnabled = false;
                return;
            }

            if( QCommon.SameText( command, "reset" ) )
            {
                _Controller.IsEnabled = true;
                if( _Controller.IsPlaying )
                    _Controller.Stop();

                _Controller.ReloadDiskInfo();
                return;
            }

            if( QCommon.SameText( command, "remap" ) )
            {
                int    ret   = QCommand.Argc - 2;
                byte[] remap = _Controller.Remap;
                if( ret <= 0 )
                {
                    for( int n = 1; n < 100; n++ )
                        if( remap[n] != n )
                            Con.Print( "  {0} -> {1}\n", n, remap[n] );
                    return;
                }

                for( int n = 1; n <= ret; n++ )
                    remap[n] = (byte) QCommon.atoi( QCommand.Argv( n + 1 ) );
                return;
            }

            if( QCommon.SameText( command, "close" ) )
            {
                _Controller.CloseDoor();
                return;
            }

            if( !_Controller.IsValidCD )
            {
                _Controller.ReloadDiskInfo();
                if( !_Controller.IsValidCD )
                {
                    Con.Print( "No CD in player.\n" );
                    return;
                }
            }

            if( QCommon.SameText( command, "play" ) )
            {
                _Controller.Play( (byte) QCommon.atoi( QCommand.Argv( 2 ) ), false );
                return;
            }

            if( QCommon.SameText( command, "loop" ) )
            {
                _Controller.Play( (byte) QCommon.atoi( QCommand.Argv( 2 ) ), true );
                return;
            }

            if( QCommon.SameText( command, "stop" ) )
            {
                _Controller.Stop();
                return;
            }

            if( QCommon.SameText( command, "pause" ) )
            {
                _Controller.Pause();
                return;
            }

            if( QCommon.SameText( command, "resume" ) )
            {
                _Controller.Resume();
                return;
            }

            if( QCommon.SameText( command, "eject" ) )
            {
                if( _Controller.IsPlaying )
                    _Controller.Stop();
                _Controller.Eject();
                return;
            }

            if( QCommon.SameText( command, "info" ) )
            {
                Con.Print( "%u tracks\n", _Controller.MaxTrack );
                if( _Controller.IsPlaying )
                    Con.Print( "Currently {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack );
                else if( _Controller.IsPaused )
                    Con.Print( "Paused {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack );
                Con.Print( "Volume is {0}\n", _Controller.Volume );
                return;
            }
        }
    }
}
