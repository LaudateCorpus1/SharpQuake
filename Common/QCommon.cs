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
    internal static class QCommon
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

        public const int PAK0_CRC = 32981;

        public static Vector3 ZeroVector = Vector3.Zero;

        // for passing as reference
        public static v3f ZeroVector3f = default( v3f );

        private static readonly byte[] ZeroBytes = new byte[4096];

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

        /// <summary>
        /// COM_LoadFile
        /// </summary>
        public static byte[] LoadFile( string path )
        {
            // look for it in the filesystem or pack files
            int length = OpenFile( path, out QDisposableWrapper<BinaryReader> file );
            if( file == null )
                return null;

            byte[] result = new byte[length];
            using( file )
            {
                Drawer.BeginDisc();
                int left = length;
                while( left > 0 )
                {
                    int count = file.Object.Read( result, length - left, left );
                    if( count == 0 )
                        sys.Error( "COM_LoadFile: reading failed!" );
                    left -= count;
                }

                Drawer.EndDisc();
            }

            return result;
        }

        /// <summary>
        /// COM_LoadPackFile
        /// Takes an explicit (not game tree related) path to a pak file.
        /// Loads the header and directory, adding the files at the beginning
        /// of the list so they override previous pack files.
        /// </summary>
        public static QPak LoadPackFile( string packfile )
        {
            FileStream file = sys.FileOpenRead( packfile );
            if( file == null )
                return null;

            QPakHeader header = sys.ReadStructure<QPakHeader>( file );

            string id = Encoding.ASCII.GetString( header.id );
            if( id != "PACK" )
                sys.Error( "{0} is not a packfile", packfile );

            header.dirofs = LittleLong( header.dirofs );
            header.dirlen = LittleLong( header.dirlen );

            int numpackfiles = header.dirlen / Marshal.SizeOf( typeof( QPakFile ) );

            if( numpackfiles > MAX_FILES_IN_PACK )
                sys.Error( "{0} has {1} files", packfile, numpackfiles );

            //if (numpackfiles != PAK0_COUNT)
            //    _IsModified = true;    // not the original file

            file.Seek( header.dirofs, SeekOrigin.Begin );
            byte[] buf = new byte[header.dirlen];
            if( file.Read( buf, 0, buf.Length ) != buf.Length )
            {
                sys.Error( "{0} buffering failed!", packfile );
            }

            List<QPakFile> info   = new List<QPakFile>( MAX_FILES_IN_PACK );
            GCHandle       handle = GCHandle.Alloc( buf, GCHandleType.Pinned );
            try
            {
                IntPtr ptr   = handle.AddrOfPinnedObject();
                int    count = 0, structSize = Marshal.SizeOf( typeof( QPakFile ) );
                while( count < header.dirlen )
                {
                    QPakFile tmp = (QPakFile) Marshal.PtrToStructure( ptr, typeof( QPakFile ) );
                    info.Add( tmp );
                    ptr   =  new IntPtr( ptr.ToInt64() + structSize );
                    count += structSize;
                }

                if( numpackfiles != info.Count )
                {
                    sys.Error( "{0} directory reading failed!", packfile );
                }
            }
            finally
            {
                handle.Free();
            }

            // crc the directory to check for modifications
            //ushort crc;
            //CRC.Init(out crc);
            //for (int i = 0; i < buf.Length; i++)
            //    CRC.ProcessByte(ref crc, buf[i]);
            //if (crc != PAK0_CRC)
            //    _IsModified = true;

            buf = null;

            // parse the directory
            QPakFileEntry[] newfiles = new QPakFileEntry[numpackfiles];
            for( int i = 0; i < numpackfiles; i++ )
            {
                QPakFileEntry pf = new QPakFileEntry();
                pf.name     = QCommon.GetString( info[i].name );
                pf.filepos  = LittleLong( info[i].filepos );
                pf.filelen  = LittleLong( info[i].filelen );
                newfiles[i] = pf;
            }

            QPak pack = new QPak( packfile, new BinaryReader( file, Encoding.ASCII ), newfiles );
            Con.Print( "Added packfile {0} ({1} files)\n", packfile, numpackfiles );
            return pack;
        }

        // COM_FOpenFile(char* filename, FILE** file)
        // If the requested file is inside a packfile, a new FILE * will be opened
        // into the file.
        public static int FOpenFile( string filename, out QDisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, true );
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

        public static bool SameText( string a, string b )
        {
            return ( string.Compare( a, b, true ) == 0 );
        }

        public static bool SameText( string a, string b, int count )
        {
            return ( string.Compare( a, 0, b, 0, count, true ) == 0 );
        }

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

        public static string Copy( string src, int maxLength )
        {
            if( src == null )
                return null;

            return ( src.Length > maxLength ? src.Substring( 1, maxLength ) : src );
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

        public static string GetString( byte[] src )
        {
            int count = 0;
            while( count < src.Length && src[count] != 0 )
                count++;

            return ( count > 0 ? Encoding.ASCII.GetString( src, 0, count ) : string.Empty );
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

        // COM_CopyFile
        //
        // Copies a file over from the net to the local cache, creating any directories
        // needed.  This is for the convenience of developers using ISDN from home.
        private static void CopyFile( string netpath, string cachepath )
        {
            using( Stream src = sys.FileOpenRead( netpath ), dest = sys.FileOpenWrite( cachepath ) )
            {
                if( src == null )
                {
                    sys.Error( "CopyFile: cannot open file {0}\n", netpath );
                }

                long   remaining = src.Length;
                string dirName   = Path.GetDirectoryName( cachepath );
                if( !Directory.Exists( dirName ) )
                    Directory.CreateDirectory( dirName );

                byte[] buf = new byte[4096];
                while( remaining > 0 )
                {
                    int count = buf.Length;
                    if( remaining < count )
                        count = (int) remaining;

                    src.Read( buf, 0, count );
                    dest.Write( buf, 0, count );
                    remaining -= count;
                }
            }
        }

        /// <summary>
        /// COM_FindFile
        /// Finds the file in the search path.
        /// </summary>
        private static int FindFile( string filename, out QDisposableWrapper<BinaryReader> file, bool duplicateStream )
        {
            file = null;

            string cachepath = string.Empty;

            //
            // search through the path, one element at a time
            //
            foreach( QPakSearchPath sp in _SearchPaths )
            {
                // is the element a pak file?
                if( sp.pack != null )
                {
                    // look through all the pak file elements
                    QPak pak = sp.pack;
                    foreach( QPakFileEntry pfile in pak.files )
                    {
                        if( pfile.name.Equals( filename ) )
                        {
                            // found it!
                            Con.DPrint( "PackFile: {0} : {1}\n", sp.pack.filename, filename );
                            if( duplicateStream )
                            {
                                FileStream pfs = (FileStream) pak.stream.BaseStream;
                                FileStream fs  = new FileStream( pfs.Name, FileMode.Open, FileAccess.Read, FileShare.Read );
                                file = new QDisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                            }
                            else
                            {
                                file = new QDisposableWrapper<BinaryReader>( pak.stream, false );
                            }

                            file.Object.BaseStream.Seek( pfile.filepos, SeekOrigin.Begin );
                            return pfile.filelen;
                        }
                    }
                }
                else
                {
                    // check a file in the directory tree
                    if( !_StaticRegistered )
                    {
                        // if not a registered version, don't ever go beyond base
                        if( filename.IndexOfAny( _Slashes ) != -1 ) // strchr (filename, '/') || strchr (filename,'\\'))
                            continue;
                    }

                    string   netpath  = sp.filename + "/" + filename; //sprintf (netpath, "%s/%s",search->filename, filename);
                    DateTime findtime = sys.GetFileTime( netpath );
                    if( findtime == DateTime.MinValue )
                        continue;

                    // see if the file needs to be updated in the cache
                    if( string.IsNullOrEmpty( _CacheDir ) ) // !com_cachedir[0])
                    {
                        cachepath = netpath; //  strcpy(cachepath, netpath);
                    }
                    else
                    {
                        if( sys.IsWindows )
                        {
                            if( netpath.Length < 2 || netpath[1] != ':' )
                                cachepath = _CacheDir + netpath;
                            else
                                cachepath = _CacheDir + netpath.Substring( 2 );
                        }
                        else
                        {
                            cachepath = _CacheDir + netpath;
                        }

                        DateTime cachetime = sys.GetFileTime( cachepath );
                        if( cachetime < findtime )
                            CopyFile( netpath, cachepath );
                        netpath = cachepath;
                    }

                    Con.DPrint( "FindFile: {0}\n", netpath );
                    FileStream fs = sys.FileOpenRead( netpath );
                    if( fs == null )
                    {
                        file = null;
                        return -1;
                    }

                    file = new QDisposableWrapper<BinaryReader>( new BinaryReader( fs, Encoding.ASCII ), true );
                    return (int) fs.Length;
                }
            }

            Con.DPrint( "FindFile: can't find {0}\n", filename );
            return -1;
        }

        // COM_OpenFile(char* filename, int* hndl)
        // filename never has a leading slash, but may contain directory walks
        // returns a handle and a length
        // it may actually be inside a pak file
        private static int OpenFile( string filename, out QDisposableWrapper<BinaryReader> file )
        {
            return FindFile( filename, out file, false );
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

        // COM_InitFilesystem
        private static void InitFileSystem()
        {
            //
            // -basedir <path>
            // Overrides the system supplied base directory (under GAMENAME)
            //
            string basedir = string.Empty;
            int    i       = CheckParm( "-basedir" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                basedir = _Argv[i + 1];
            }
            else
            {
                basedir              = host.Params.basedir;
                qparam.globalbasedir = basedir;
            }

            if( !string.IsNullOrEmpty( basedir ) )
            {
                basedir.TrimEnd( '\\', '/' );
            }

            //
            // -cachedir <path>
            // Overrides the system supplied cache directory (NULL or /qcache)
            // -cachedir - will disable caching.
            //
            i = CheckParm( "-cachedir" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                if( _Argv[i + 1][0] == '-' )
                    _CacheDir = string.Empty;
                else
                    _CacheDir = _Argv[i + 1];
            }
            else if( !string.IsNullOrEmpty( host.Params.cachedir ) )
            {
                _CacheDir = host.Params.cachedir;
            }
            else
            {
                _CacheDir = string.Empty;
            }

            //
            // start up with GAMENAME by default (id1)
            //
            AddGameDirectory( basedir + "/" + QDef.GAMENAME );
            qparam.globalgameid = QDef.GAMENAME;

            if( HasParam( "-rogue" ) )
            {
                AddGameDirectory( basedir + "/rogue" );
                qparam.globalgameid = "rogue";
            }

            if( HasParam( "-hipnotic" ) )
            {
                AddGameDirectory( basedir + "/hipnotic" );
                qparam.globalgameid = "hipnotic";
            }

            //
            // -game <gamedir>
            // Adds basedir/gamedir as an override game
            //
            i = CheckParm( "-game" );
            if( ( i > 0 ) && ( i < _Argv.Length - 1 ) )
            {
                _IsModified = true;
                AddGameDirectory( basedir + "/" + _Argv[i + 1] );
            }

            //
            // -path <dir or packfile> [<dir or packfile>] ...
            // Fully specifies the exact serach path, overriding the generated one
            //
            i = CheckParm( "-path" );
            if( i > 0 )
            {
                _IsModified = true;
                _SearchPaths.Clear();
                while( ++i < _Argv.Length )
                {
                    if( string.IsNullOrEmpty( _Argv[i] ) || _Argv[i][0] == '+' || _Argv[i][0] == '-' )
                        break;

                    _SearchPaths.Insert( 0, new QPakSearchPath( _Argv[i] ) );
                }
            }
        }

        // COM_AddGameDirectory
        //
        // Sets com_gamedir, adds the directory to the head of the path,
        // then loads and adds pak1.pak pak2.pak ...
        private static void AddGameDirectory( string dir )
        {
            _GameDir = dir;

            //
            // add the directory to the search path
            //
            _SearchPaths.Insert( 0, new QPakSearchPath( dir ) );

            //
            // add any pak files in the format pak0.pak pak1.pak, ...
            //
            for( int i = 0;; i++ )
            {
                string pakfile = $"{dir}/PAK{i}.PAK";
                QPak   pak     = LoadPackFile( pakfile );
                if( pak == null )
                    break;

                _SearchPaths.Insert( 0, new QPakSearchPath( pak ) );
            }
        }

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
    }
}
