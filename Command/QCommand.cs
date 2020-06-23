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

using System.Collections.Generic;
using System.Text;

namespace SharpQuake
{
    // Command execution takes a string, breaks it into tokens,
    // then searches for a command or variable that matches the first token.
    //
    // Commands can come from three sources, but the handler functions may choose
    // to dissallow the action or forward it to a remote server if the source is
    // not apropriate.


    internal static class QCommand
    {
        public static QCommandSource Source => _Source;

        public static int Argc => _Argc;

        // char	*Cmd_Args (void);
        public static string Args => _Args;

        internal static bool Wait { get => _Wait; set => _Wait = value; }

        private const int MAX_ALIAS_NAME = 32;
        private const int MAX_ARGS       = 80;

        private static QCommandSource                       _Source; // extern	QCommandSource	cmd_source;
        private static Dictionary<string, string>           _Aliases;
        private static Dictionary<string, QCommandDelegate> _Functions;
        private static int                                  _Argc;
        private static string[]                             _Argv; // char	*cmd_argv[MAX_ARGS];
        private static string                               _Args; // char* cmd_args = NULL;
        private static bool                                 _Wait; // qboolean cmd_wait;

        public static void Init()
        {
            //
            // register our commands
            //
            Add( "stuffcmds", StuffCmds_f );
            Add( "exec",      Exec_f );
            Add( "echo",      Echo_f );
            Add( "alias",     Alias_f );
            Add( "QCommand",  ForwardToServer );
            Add( "wait",      QCommandBuffer.Cmd_Wait_f ); // todo: move to QCommandBuffer class?
        }

        // called by the init functions of other parts of the program to
        // register commands and functions to call for them.
        public static void Add( string name, QCommandDelegate function )
        {
            // ??? because hunk allocation would get stomped
            if( host.IsInitialized )
                sys.Error( "Cmd.Add after host initialized!" );

            // fail if the command is a variable name
            if( cvar.Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined as a var!\n", name );
                return;
            }

            // fail if the command already exists
            if( Exists( name ) )
            {
                Con.Print( "Cmd.Add: {0} already defined!\n", name );
                return;
            }

            _Functions.Add( name, function );
        }

        // attempts to match a partial command for automatic command line completion
        // returns NULL if nothing fits
        public static string[] Complete( string partial )
        {
            if( string.IsNullOrEmpty( partial ) )
                return null;

            List<string> result = new List<string>();
            foreach( string cmd in _Functions.Keys )
            {
                if( cmd.StartsWith( partial ) )
                    result.Add( cmd );
            }

            return ( result.Count > 0 ? result.ToArray() : null );
        }

        // will return an empty string, not a NULL
        // if arg > argc, so string operations are allways safe.
        public static string Argv( int arg )
        {
            if( arg < 0 || arg >= _Argc )
                return string.Empty;

            return _Argv[arg];
        }

        public static bool Exists( string name )
        {
            return ( Find( name ) != null );
        }

        // Takes a null terminated string.  Does not need to be /n terminated.
        // breaks the string up into arg tokens.
        // Parses the given string into command line tokens.
        public static void TokenizeString( string text )
        {
            // clear the args from the last string
            _Argc = 0;
            _Args = null;
            _Argv = null;

            List<string> argv = new List<string>( MAX_ARGS );
            while( !string.IsNullOrEmpty( text ) )
            {
                if( _Argc == 1 )
                    _Args = text;

                text = QCommon.Parse( text );

                if( string.IsNullOrEmpty( QCommon.Token ) )
                    break;

                if( _Argc < MAX_ARGS )
                {
                    argv.Add( QCommon.Token );
                    _Argc++;
                }
            }

            _Argv = argv.ToArray();
        }

        // void	Cmd_ExecuteString (char *text, QCommandSource src);
        // Parses a single line of text into arguments and tries to execute it.
        // The text can come from the command buffer, a remote QClient, or stdin.
        //
        // A complete command line has been parsed, so try to execute it
        // $TODO: lookupnoadd the token to speed search?
        public static void ExecuteString( string text, QCommandSource src )
        {
            _Source = src;

            TokenizeString( text );

            // execute the command line
            if( _Argc <= 0 )
                return; // no tokens

            // check functions
            QCommandDelegate handler = Find( _Argv[0] ); // must search with comparison like Q_strcasecmp()
            if( handler != null )
            {
                handler();
            }
            else
            {
                // check alias
                string alias = FindAlias( _Argv[0] ); // must search with compare func like Q_strcasecmp
                if( !string.IsNullOrEmpty( alias ) )
                {
                    QCommandBuffer.InsertText( alias );
                }
                else
                {
                    // check cvars
                    if( !cvar.Command() )
                        Con.Print( "Unknown command \"{0}\"\n", _Argv[0] );
                }
            }
        }

        // adds the current command line as a clc_stringcmd to the QClient message.
        // things like godmode, noclip, etc, are commands directed to the server,
        // so when they are typed in at the console, they will need to be forwarded.
        //
        // Sends the entire command line over to the server
        public static void ForwardToServer()
        {
            if( QClient.cls.state != ServerType.CONNECTED )
            {
                Con.Print( "Can't \"{0}\", not connected\n", QCommand.Argv( 0 ) );
                return;
            }

            if( QClient.cls.demoplayback )
                return; // not really connected

            QMessageWriter writer = QClient.cls.message;
            writer.WriteByte( protocol.clc_stringcmd );
            if( !QCommand.Argv( 0 ).Equals( "QCommand" ) )
            {
                writer.Print( QCommand.Argv( 0 ) + " " );
            }

            if( QCommand.Argc > 1 )
            {
                writer.Print( QCommand.Args );
            }
            else
            {
                writer.Print( "\n" );
            }
        }

        public static string JoinArgv()
        {
            return string.Join( " ", _Argv );
        }

        private static QCommandDelegate Find( string name )
        {
            _Functions.TryGetValue( name, out QCommandDelegate result );
            return result;
        }

        private static string FindAlias( string name )
        {
            _Aliases.TryGetValue( name, out string result );
            return result;
        }

        /// <summary>
        /// Adds command line parameters as script statements
        /// Commands lead with a +, and continue until a - or another +
        /// quake +prog jctest.qp +QCommand amlev1
        /// quake -nosound +QCommand amlev1
        /// </summary>
        private static void StuffCmds_f()
        {
            if( _Argc != 1 )
            {
                Con.Print( "stuffcmds : execute command line parameters\n" );
                return;
            }

            // build the combined string to parse from
            StringBuilder sb = new StringBuilder( 1024 );
            for( int i = 1; i < _Argc; i++ )
            {
                if( !string.IsNullOrEmpty( _Argv[i] ) )
                {
                    sb.Append( _Argv[i] );
                    if( i + 1 < _Argc )
                        sb.Append( " " );
                }
            }

            // pull out the commands
            string text = sb.ToString();
            sb.Length = 0;

            for( int i = 0; i < text.Length; i++ )
            {
                if( text[i] == '+' )
                {
                    i++;

                    int j = i;
                    while( ( j < text.Length ) && ( text[j] != '+' ) && ( text[j] != '-' ) )
                    {
                        j++;
                    }

                    sb.Append( text.Substring( i, j - i + 1 ) );
                    sb.AppendLine();
                    i = j - 1;
                }
            }

            if( sb.Length > 0 )
            {
                QCommandBuffer.InsertText( sb.ToString() );
            }
        }

        private static void Exec_f()
        {
            if( _Argc != 2 )
            {
                Con.Print( "exec <filename> : execute a script file\n" );
                return;
            }

            byte[] bytes = QCommon.LoadFile( _Argv[1] );
            if( bytes == null )
            {
                Con.Print( "couldn't exec {0}\n", _Argv[1] );
                return;
            }

            string script = Encoding.ASCII.GetString( bytes );
            Con.Print( "execing {0}\n", _Argv[1] );
            QCommandBuffer.InsertText( script );
        }

        // Just prints the rest of the line to the console
        private static void Echo_f()
        {
            for( int i = 1; i < _Argc; i++ )
            {
                Con.Print( "{0} ", _Argv[i] );
            }

            Con.Print( "\n" );
        }

        // Creates a new command that executes a command string (possibly ; seperated)
        private static void Alias_f()
        {
            if( _Argc == 1 )
            {
                Con.Print( "Current alias commands:\n" );
                foreach( KeyValuePair<string, string> alias in _Aliases )
                {
                    Con.Print( "{0} : {1}\n", alias.Key, alias.Value );
                }

                return;
            }

            string name = _Argv[1];
            if( name.Length >= MAX_ALIAS_NAME )
            {
                Con.Print( "Alias name is too long\n" );
                return;
            }

            // copy the rest of the command line
            StringBuilder sb = new StringBuilder( 1024 );
            for( int i = 2; i < _Argc; i++ )
            {
                sb.Append( _Argv[i] );
                if( i + 1 < _Argc )
                    sb.Append( " " );
            }

            sb.AppendLine();
            _Aliases[name] = sb.ToString();
        }

        static QCommand()
        {
            _Aliases   = new Dictionary<string, string>();
            _Functions = new Dictionary<string, QCommandDelegate>();
        }
    }
}
