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

//
// Source: QCommon.h + QCommon.c
//

// All of Quake's data access is through a hierarchical file system,
// but the contents of the file system can be transparently merged from several sources.
//
// The "base directory" is the path to the directory holding the quake.exe and all game directories.
// The sys_* files pass this to host_init in quakeparms_t->basedir.
// This can be overridden with the "-basedir" command line parm to allow code debugging in a different directory.
// The base directory is only used during filesystem initialization.
//
// The "game directory" is the first tree on the search path and directory that all generated
// files (savegames, screenshots, demos, config files) will be saved to.
// This can be overridden with the "-game" command line parameter.
// The game directory can never be changed while quake is executing.
// This is a precacution against having a malicious server instruct clients to write files over areas they shouldn't.
//
// The "cache directory" is only used during development to save network bandwidth, especially over ISDN / T1 lines.
// If there is a cache directory specified, when a file is found by the normal search path, it will be mirrored
// into the cache directory, then opened there.
//
//
//
// $TODO:
// The file "parms.txt" will be read out of the game directory and appended to the current command line arguments to
// allow different games to initialize startup parms differently.
// This could be used to add a "-sspeed 22050" for the high quality sound edition.
// Because they are added at the end, they will not override an explicit setting on the original command line.
// $TODO: Split out code some, this file is pretty bloated as it is.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenTK;

namespace SharpQuake
{
    internal static partial class QCommon
    {
        public static bool IsBigEndian => !BitConverter.IsLittleEndian;

        public static string GameDir => _GameDir;

        public static QGameType GameType => _GameType;

        public static int Argc => _Argv.Length;

        public static string[] Args
        {
            get => _Argv;
            set
            {
                _Argv = new string[value.Length];
                value.CopyTo( _Argv, 0 );
                _Args = string.Join( " ", value );
            }
        }

        public static string Token => _Token;

        public static bool IsRegistered => Math.Abs( _Registered.Value ) > 0.001f;

        public const int MAX_FILES_IN_PACK = 2048;

        // if a packfile directory differs from this, it is assumed to be hacked
        public const int PAK0_COUNT = 339;
        public const int PAK0_CRC   = 32981;

        // this graphic needs to be in the pak file to use registered features
        private static ushort[] _Pop = new ushort[]
        {
            0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x6600, 0x0000, 0x0000, 0x0000, 0x6600, 0x0000, 0x0000, 0x0066, 0x0000, 0x0000, 0x0000, 0x0000, 0x0067,
            0x0000, 0x0000, 0x6665, 0x0000, 0x0000, 0x0000, 0x0000, 0x0065, 0x6600, 0x0063, 0x6561, 0x0000, 0x0000, 0x0000, 0x0000, 0x0061, 0x6563, 0x0064, 0x6561, 0x0000, 0x0000, 0x0000, 0x0000,
            0x0061, 0x6564, 0x0064, 0x6564, 0x0000, 0x6469, 0x6969, 0x6400, 0x0064, 0x6564, 0x0063, 0x6568, 0x6200, 0x0064, 0x6864, 0x0000, 0x6268, 0x6563, 0x0000, 0x6567, 0x6963, 0x0064, 0x6764,
            0x0063, 0x6967, 0x6500, 0x0000, 0x6266, 0x6769, 0x6a68, 0x6768, 0x6a69, 0x6766, 0x6200, 0x0000, 0x0062, 0x6566, 0x6666, 0x6666, 0x6666, 0x6562, 0x0000, 0x0000, 0x0000, 0x0062, 0x6364,
            0x6664, 0x6362, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0062, 0x6662, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0061, 0x6661, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
            0x0000, 0x6500, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x6400, 0x0000, 0x0000, 0x0000
        };

        // for passing as reference
        private static string[] safeargvs = new string[] { "-stdvid", "-nolan", "-nosound", "-nocdaudio", "-nojoy", "-nomouse", "-dibonly" };

        private static IByteOrderConverter  _Converter;
        private static cvar                 _Registered;
        private static cvar                 _CmdLine;
        private static string               _CacheDir;    // com_cachedir[MAX_OSPATH];
        private static string               _GameDir;     // com_gamedir[MAX_OSPATH];
        private static List<QPakSearchPath> _SearchPaths; // QPakSearchPath    *com_searchpaths;
        private static string[]             _Argv;
        private static string               _Args;             // com_cmdline
        private static QGameType            _GameType;         // qboolean		standard_quake = true, rogue, hipnotic;
        private static bool                 _IsModified;       // com_modified
        private static bool                 _StaticRegistered; // static_registered
        private static char[]               _Slashes = new char[] { '/', '\\' };
        private static string               _Token; // com_token

        static QCommon()
        {
            // set the byte swapping variables in a portable manner
            if( BitConverter.IsLittleEndian )
            {
                _Converter = new QLittleEndianConverter();
            }
            else
            {
                _Converter = new QBigEndianConverter();
            }

            _SearchPaths = new List<QPakSearchPath>();
        }

        // void COM_Init (char *path)
        public static void Init( string path, string[] argv )
        {
            _Argv       = argv;
            _Registered = new cvar( "registered", "0" );
            _CmdLine    = new cvar( "cmdline",    "0", false, true );

            QCommand.Add( "path", Path_f );

            InitFileSystem();
            CheckRegistered();
        }

        public static string Argv( int index )
        {
            return _Argv[index];
        }

        // int COM_CheckParm (char *parm)
        // Returns the position (1 to argc-1) in the program's argument list
        // where the given parameter apears, or 0 if not present
        public static int CheckParm( string parm )
        {
            for( int i = 1; i < _Argv.Length; i++ )
            {
                if( _Argv[i].Equals( parm ) )
                    return i;
            }

            return 0;
        }

        public static bool HasParam( string parm )
        {
            return ( CheckParm( parm ) > 0 );
        }

        // void COM_InitArgv (int argc, char **argv)
        public static void InitArgv( string[] argv )
        {
            // reconstitute the command line for the cmdline externally visible cvar
            _Args = string.Join( " ", argv );
            _Argv = new string[argv.Length];
            argv.CopyTo( _Argv, 0 );

            bool safe = false;
            foreach( string arg in _Argv )
            {
                if( arg == "-safe" )
                {
                    safe = true;
                    break;
                }
            }

            if( safe )
            {
                // force all the safe-mode switches. Note that we reserved extra space in
                // case we need to add these, so we don't need an overflow check
                string[] largv = new string[_Argv.Length + safeargvs.Length];
                _Argv.CopyTo( largv, 0 );
                safeargvs.CopyTo( largv, _Argv.Length );
                _Argv = largv;
            }

            _GameType = QGameType.StandardQuake;

            if( HasParam( "-rogue" ) )
                _GameType = QGameType.Rogue;

            if( HasParam( "-hipnotic" ) )
                _GameType = QGameType.Hipnotic;
        }

        /// <summary>
        /// COM_Parse
        /// Parse a token out of a string
        /// </summary>
        public static string Parse( string data )
        {
            _Token = string.Empty;

            if( string.IsNullOrEmpty( data ) )
                return null;

            // skip whitespace
            int i = 0;
            while( i < data.Length )
            {
                while( i < data.Length )
                {
                    if( data[i] > ' ' )
                        break;

                    i++;
                }

                if( i >= data.Length )
                    return null;

                // skip // comments
                if( ( data[i] == '/' ) && ( i + 1 < data.Length ) && ( data[i + 1] == '/' ) )
                {
                    while( i < data.Length && data[i] != '\n' )
                        i++;
                }
                else
                    break;
            }

            if( i >= data.Length )
                return null;

            int i0 = i;

            // handle quoted strings specially
            if( data[i] == '\"' )
            {
                i++;
                i0 = i;
                while( i < data.Length && data[i] != '\"' )
                    i++;

                if( i == data.Length )
                {
                    _Token = data.Substring( i0, i - i0 );
                    return null;
                }
                else
                {
                    _Token = data.Substring( i0, i - i0 );
                    return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
                }
            }

            // parse single characters
            char c = data[i];
            if( c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
            {
                _Token = data.Substring( i, 1 );
                return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
            }

            // parse a regular word
            while( i < data.Length )
            {
                c = data[i];
                if( c <= 32 || c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':' )
                {
                    i--;
                    break;
                }

                i++;
            }

            if( i == data.Length )
            {
                _Token = data.Substring( i0, i - i0 );
                return null;
            }

            _Token = data.Substring( i0, i                   - i0 + 1 );
            return ( i + 1 < data.Length ? data.Substring( i + 1 ) : null );
        }


        public static void FillArray<T>( T[] dest, T value )
        {
            int elementSizeInBytes = Marshal.SizeOf( typeof( T ) );
            int blockSize          = Math.Min( dest.Length, 4096 / elementSizeInBytes );
            for( int i = 0; i < blockSize; i++ )
                dest[i] = value;

            int blockSizeInBytes = blockSize * elementSizeInBytes;
            int offset           = blockSizeInBytes;
            int lengthInBytes    = Buffer.ByteLength( dest );
            while( true ) // offset + blockSize <= lengthInBytes)
            {
                int left = lengthInBytes - offset;
                if( left < blockSizeInBytes )
                    blockSizeInBytes = left;

                if( blockSizeInBytes <= 0 )
                    break;

                Buffer.BlockCopy( dest, 0, dest, offset, blockSizeInBytes );
                offset += blockSizeInBytes;
            }
        }

        public static void ZeroArray<T>( T[] dest, int startIndex, int length )
        {
            int elementBytes = Marshal.SizeOf( typeof( T ) );
            int offset       = startIndex * elementBytes;
            int sizeInBytes  = dest.Length * elementBytes - offset;
            while( true )
            {
                int blockSize = sizeInBytes - offset;
                if( blockSize > ZeroBytes.Length )
                    blockSize = ZeroBytes.Length;

                if( blockSize <= 0 )
                    break;

                Buffer.BlockCopy( ZeroBytes, 0, dest, offset, blockSize );
                offset += blockSize;
            }
        }


        // COM_Path_f
        private static void Path_f()
        {
            Con.Print( "Current search path:\n" );
            foreach( QPakSearchPath sp in _SearchPaths )
            {
                if( sp.pack != null )
                {
                    Con.Print( "{0} ({1} files)\n", sp.pack.filename, sp.pack.files.Length );
                }
                else
                {
                    Con.Print( "{0}\n", sp.filename );
                }
            }
        }

        // COM_CheckRegistered
        //
        // Looks for the pop.txt file and verifies it.
        // Sets the "registered" cvar.
        // Immediately exits out if an alternate game was attempted to be started without
        // being registered.
        private static void CheckRegistered()
        {
            _StaticRegistered = false;

            byte[] buf = LoadFile( "gfx/pop.lmp" );
            if( buf == null || buf.Length < 256 )
            {
                Con.Print( "Playing shareware version.\n" );
                if( _IsModified )
                    sys.Error( "You must have the registered version to use modified games" );
                return;
            }

            ushort[] check = new ushort[buf.Length / 2];
            Buffer.BlockCopy( buf, 0, check, 0, buf.Length );
            for( int i = 0; i < 128; i++ )
            {
                if( _Pop[i] != (ushort) _Converter.BigShort( (short) check[i] ) )
                    sys.Error( "Corrupted data file." );
            }

            cvar.Set( "cmdline",    _Args );
            cvar.Set( "registered", "1" );
            _StaticRegistered = true;
            Con.Print( "Playing registered version.\n" );
        }
    }
}
