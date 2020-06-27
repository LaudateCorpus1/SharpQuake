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
    //Any number of commands can be added in a frame, from several different sources.
    //Most commands come from either keybindings or console line QInput, but remote
    //servers can also send across commands and entire text files can be execed.

    //The + command line options are also added to the command buffer.

    //The game starts with a Cbuf_AddText ("exec quake.rc\n"); Cbuf_Execute ();

    internal static class QCommandBuffer
    {
        private static StringBuilder _Buf;
        private static bool          _Wait;

        // Cbuf_Init()
        // allocates an initial text buffer that will grow as needed
        public static void Init()
        {
            QCommand.Add( "wait", Cmd_Wait_f );
        }

        // Cbuf_AddText()
        // as new commands are generated from the console or keybindings,
        // the text is added to the end of the command buffer.
        public static void AddText( string text )
        {
            if( string.IsNullOrEmpty( text ) )
                return;

            int len = text.Length;
            if( _Buf.Length + len > _Buf.Capacity )
            {
                QConsole.Print( "QCommandBuffer.AddText: overflow!\n" );
            }
            else
            {
                _Buf.Append( text );
            }
        }

        // Cbuf_InsertText()
        // when a command wants to issue other commands immediately, the text is
        // inserted at the beginning of the buffer, before any remaining unexecuted
        // commands.
        // Adds command text immediately after the current command
        // ???Adds a \n to the text
        // FIXME: actually change the command buffer to do less copying
        public static void InsertText( string text )
        {
            _Buf.Insert( 0, text );
        }

        // Cbuf_Execute()
        // Pulls off \n terminated lines of text from the command buffer and sends
        // them through Cmd_ExecuteString.  Stops when the buffer is empty.
        // Normally called once per frame, but may be explicitly invoked.
        // Do not call inside a command function!
        public static void Execute()
        {
            while( _Buf.Length > 0 )
            {
                string text = _Buf.ToString();

                // find a \n or ; line break
                int quotes = 0, i;
                for( i = 0; i < text.Length; i++ )
                {
                    if( text[i] == '"' )
                        quotes++;

                    if( ( ( quotes & 1 ) == 0 ) && ( text[i] == ';' ) )
                        break; // don't break if inside a quoted string

                    if( text[i] == '\n' )
                        break;
                }

                string line = text.Substring( 0, i ).TrimEnd( '\n', ';' );

                // delete the text from the command buffer and move remaining commands down
                // this is necessary because commands (exec, alias) can insert data at the
                // beginning of the text buffer

                if( i == _Buf.Length )
                {
                    _Buf.Length = 0;
                }
                else
                {
                    _Buf.Remove( 0, i + 1 );
                }

                // execute the command line
                if( !string.IsNullOrEmpty( line ) )
                {
                    QCommand.ExecuteString( line, QCommandSource.src_command );

                    if( _Wait )
                    {
                        // skip out while text still remains in buffer, leaving it
                        // for next frame
                        _Wait = false;
                        break;
                    }
                }
            }
        }

        // Cmd_Wait_f
        // Causes execution of the remainder of the command buffer to be delayed until
        // next frame.  This allows commands like:
        // bind g "impulse 5 ; +attack ; wait ; -attack ; impulse 2"
        public static void Cmd_Wait_f()
        {
            _Wait = true;
        }

        static QCommandBuffer()
        {
            _Buf = new StringBuilder( 8192 ); // space for commands and script files
        }
    }
}
