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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpQuake
{
    internal static partial class QCommon
    {
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
    }
}
