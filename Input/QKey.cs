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
using System.Text;

// keys.h
// keys.c

// key up events are sent even if in console mode

namespace SharpQuake
{
    /// <summary>
    /// Key_functions
    /// </summary>
    internal static partial class QKey
    {
        public static QKeyDest Destination { get => _KeyDest; set => _KeyDest = value; }

        public static bool TeamMessage { get => _TeamMessage; set => _TeamMessage = value; }

        public static char[][] Lines      => _Lines;
        public static int      EditLine   => _EditLine;
        public static string   ChatBuffer => _ChatBuffer.ToString();
        public static int      LastPress  => _LastPress;
        public static string[] Bindings   => _Bindings;

        //
        // these are the key numbers that should be passed to Key_Event
        //
        public const int K_TAB       = 9;
        public const int K_ENTER     = 13;
        public const int K_ESCAPE    = 27;
        public const int K_SPACE     = 32;
        public const int K_BACKSPACE = 127;

        // normal keys should be passed as lowercased ascii
        public const int K_UPARROW    = 128;
        public const int K_DOWNARROW  = 129;
        public const int K_LEFTARROW  = 130;
        public const int K_RIGHTARROW = 131;

        public const int K_ALT   = 132;
        public const int K_CTRL  = 133;
        public const int K_SHIFT = 134;

        public const int K_F1  = 135;
        public const int K_F2  = 136;
        public const int K_F3  = 137;
        public const int K_F4  = 138;
        public const int K_F5  = 139;
        public const int K_F6  = 140;
        public const int K_F7  = 141;
        public const int K_F8  = 142;
        public const int K_F9  = 143;
        public const int K_F10 = 144;
        public const int K_F11 = 145;
        public const int K_F12 = 146;

        public const int K_INS   = 147;
        public const int K_DEL   = 148;
        public const int K_PGDN  = 149;
        public const int K_PGUP  = 150;
        public const int K_HOME  = 151;
        public const int K_END   = 152;
        public const int K_PAUSE = 255;

        //
        // mouse buttons generate virtual keys
        //
        public const int K_MOUSE1 = 200;
        public const int K_MOUSE2 = 201;
        public const int K_MOUSE3 = 202;

        //
        // joystick buttons
        //
        public const int K_JOY1 = 203;
        public const int K_JOY2 = 204;
        public const int K_JOY3 = 205;
        public const int K_JOY4 = 206;

        //
        // aux keys are for multi-buttoned joysticks to generate so they can use
        // the normal binding process
        //
        public const int K_AUX1  = 207;
        public const int K_AUX2  = 208;
        public const int K_AUX3  = 209;
        public const int K_AUX4  = 210;
        public const int K_AUX5  = 211;
        public const int K_AUX6  = 212;
        public const int K_AUX7  = 213;
        public const int K_AUX8  = 214;
        public const int K_AUX9  = 215;
        public const int K_AUX10 = 216;
        public const int K_AUX11 = 217;
        public const int K_AUX12 = 218;
        public const int K_AUX13 = 219;
        public const int K_AUX14 = 220;
        public const int K_AUX15 = 221;
        public const int K_AUX16 = 222;
        public const int K_AUX17 = 223;
        public const int K_AUX18 = 224;
        public const int K_AUX19 = 225;
        public const int K_AUX20 = 226;
        public const int K_AUX21 = 227;
        public const int K_AUX22 = 228;
        public const int K_AUX23 = 229;
        public const int K_AUX24 = 230;
        public const int K_AUX25 = 231;
        public const int K_AUX26 = 232;
        public const int K_AUX27 = 233;
        public const int K_AUX28 = 234;
        public const int K_AUX29 = 235;
        public const int K_AUX30 = 236;
        public const int K_AUX31 = 237;
        public const int K_AUX32 = 238;

        public const int K_MWHEELUP = 239;

        // JACK: Intellimouse(c) Mouse Wheel Support
        public const int K_MWHEELDOWN = 240;

        public static int LinePos;
        public static int KeyCount;

        private const int MAXCMDLINE = 256;

        private static QKeyName[] _KeyNames = new QKeyName[]
        {
            new QKeyName( "TAB", K_TAB ), new QKeyName( "ENTER", K_ENTER ), new QKeyName( "ESCAPE", K_ESCAPE ), new QKeyName( "SPACE", K_SPACE ), new QKeyName( "BACKSPACE", K_BACKSPACE ),
            new QKeyName( "UPARROW", K_UPARROW ), new QKeyName( "DOWNARROW", K_DOWNARROW ), new QKeyName( "LEFTARROW", K_LEFTARROW ), new QKeyName( "RIGHTARROW", K_RIGHTARROW ),
            new QKeyName( "ALT", K_ALT ), new QKeyName( "CTRL", K_CTRL ), new QKeyName( "SHIFT", K_SHIFT ), new QKeyName( "F1", K_F1 ), new QKeyName( "F2", K_F2 ), new QKeyName( "F3", K_F3 ),
            new QKeyName( "F4", K_F4 ), new QKeyName( "F5", K_F5 ), new QKeyName( "F6", K_F6 ), new QKeyName( "F7", K_F7 ), new QKeyName( "F8", K_F8 ), new QKeyName( "F9", K_F9 ),
            new QKeyName( "F10", K_F10 ), new QKeyName( "F11", K_F11 ), new QKeyName( "F12", K_F12 ), new QKeyName( "INS", K_INS ), new QKeyName( "DEL", K_DEL ), new QKeyName( "PGDN", K_PGDN ),
            new QKeyName( "PGUP", K_PGUP ), new QKeyName( "HOME", K_HOME ), new QKeyName( "END", K_END ), new QKeyName( "MOUSE1", K_MOUSE1 ), new QKeyName( "MOUSE2", K_MOUSE2 ),
            new QKeyName( "MOUSE3", K_MOUSE3 ), new QKeyName( "JOY1", K_JOY1 ), new QKeyName( "JOY2", K_JOY2 ), new QKeyName( "JOY3", K_JOY3 ), new QKeyName( "JOY4", K_JOY4 ),
            new QKeyName( "AUX1", K_AUX1 ), new QKeyName( "AUX2", K_AUX2 ), new QKeyName( "AUX3", K_AUX3 ), new QKeyName( "AUX4", K_AUX4 ), new QKeyName( "AUX5", K_AUX5 ),
            new QKeyName( "AUX6", K_AUX6 ), new QKeyName( "AUX7", K_AUX7 ), new QKeyName( "AUX8", K_AUX8 ), new QKeyName( "AUX9", K_AUX9 ), new QKeyName( "AUX10", K_AUX10 ),
            new QKeyName( "AUX11", K_AUX11 ), new QKeyName( "AUX12", K_AUX12 ), new QKeyName( "AUX13", K_AUX13 ), new QKeyName( "AUX14", K_AUX14 ), new QKeyName( "AUX15", K_AUX15 ),
            new QKeyName( "AUX16", K_AUX16 ), new QKeyName( "AUX17", K_AUX17 ), new QKeyName( "AUX18", K_AUX18 ), new QKeyName( "AUX19", K_AUX19 ), new QKeyName( "AUX20", K_AUX20 ),
            new QKeyName( "AUX21", K_AUX21 ), new QKeyName( "AUX22", K_AUX22 ), new QKeyName( "AUX23", K_AUX23 ), new QKeyName( "AUX24", K_AUX24 ), new QKeyName( "AUX25", K_AUX25 ),
            new QKeyName( "AUX26", K_AUX26 ), new QKeyName( "AUX27", K_AUX27 ), new QKeyName( "AUX28", K_AUX28 ), new QKeyName( "AUX29", K_AUX29 ), new QKeyName( "AUX30", K_AUX30 ),
            new QKeyName( "AUX31", K_AUX31 ), new QKeyName( "AUX32", K_AUX32 ), new QKeyName( "PAUSE", K_PAUSE ), new QKeyName( "MWHEELUP", K_MWHEELUP ), new QKeyName( "MWHEELDOWN", K_MWHEELDOWN ),
            new QKeyName( "SEMICOLON", ';' ), // because a raw semicolon seperates commands
        };

        private static char[][] _Lines = new char[32][]; //, MAXCMDLINE]; // char	key_lines[32][MAXCMDLINE];

        // key_linepos
        private static bool _ShiftDown; // = false;

        private static int _LastPress; // key_lastpress

        private static int _EditLine;    // edit_line=0;
        private static int _HistoryLine; // history_line=0;

        private static QKeyDest _KeyDest; // key_dest

        // key_count			// incremented every key event

        private static string[] _Bindings    = new string[256]; // char	*keybindings[256];
        private static bool[]   _ConsoleKeys = new bool[256];   // consolekeys[256]	// if true, can't be rebound while in console
        private static bool[]   _MenuBound   = new bool[256];   // menubound[256]	// if true, can't be rebound while in menu
        private static int[]    _KeyShift    = new int[256];    // keyshift[256]		// key to map to if shift held down in console
        private static int[]    _Repeats     = new int[256];    // key_repeats[256]	// if > 1, it is autorepeating
        private static bool[]   _KeyDown     = new bool[256];

        private static StringBuilder _ChatBuffer = new StringBuilder( 32 ); // chat_buffer
        private static bool          _TeamMessage;                          // qboolean team_message = false;

        // Key_Event (int key, qboolean down)
        //
        // Called by the system between frames for both key up and key down events
        // Should NOT be called during an interrupt!
        public static void Event( int key, bool down )
        {
            _KeyDown[key] = down;

            if( !down )
                _Repeats[key] = 0;

            _LastPress = key;
            KeyCount++;
            if( KeyCount <= 0 )
                return; // just catching keys for Con_NotifyBox

            // update auto-repeat status
            if( down )
            {
                _Repeats[key]++;
                if( key != K_BACKSPACE && key != K_PAUSE && key != K_PGUP && key != K_PGDN && _Repeats[key] > 1 )
                {
                    return; // ignore most autorepeats
                }

                if( key >= 200 && string.IsNullOrEmpty( _Bindings[key] ) )
                    QConsole.Print( "{0} is unbound, hit F4 to set.\n", KeynumToString( key ) );
            }

            if( key == K_SHIFT )
                _ShiftDown = down;

            //
            // handle escape specialy, so the user can never unbind it
            //
            if( key == K_ESCAPE )
            {
                if( !down )
                    return;

                switch( _KeyDest )
                {
                    case QKeyDest.Message:
                        KeyMessage( key );
                        break;

                    case QKeyDest.Menu:
                        menu.KeyDown( key );
                        break;

                    case QKeyDest.Game:
                    case QKeyDest.Console:
                        menu.ToggleMenu_f();
                        break;

                    default:
                        sys.Error( "Bad key_dest" );
                        break;
                }

                return;
            }

            //
            // key up events only generate commands if the game key binding is
            // a button command (leading + sign).  These will occur even in console mode,
            // to keep the character from continuing an action started before a console
            // switch.  Button commands include the keynum as a parameter, so multiple
            // downs can be matched with ups
            //
            if( !down )
            {
                string kb = _Bindings[key];
                if( !string.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                {
                    QCommandBuffer.AddText( $"-{kb.Substring( 1 )} {key}\n" );
                }

                if( _KeyShift[key] != key )
                {
                    kb = _Bindings[_KeyShift[key]];
                    if( !string.IsNullOrEmpty( kb ) && kb.StartsWith( "+" ) )
                        QCommandBuffer.AddText( $"-{kb.Substring( 1 )} {key}\n" );
                }

                return;
            }

            //
            // during demo playback, most keys bring up the main menu
            //
            if( QClient.cls.demoplayback && down && _ConsoleKeys[key] && _KeyDest == QKeyDest.Game )
            {
                menu.ToggleMenu_f();
                return;
            }

            //
            // if not a consolekey, send to the interpreter no matter what mode is
            //
            if( ( _KeyDest == QKeyDest.Menu    && _MenuBound[key] )    ||
                ( _KeyDest == QKeyDest.Console && !_ConsoleKeys[key] ) ||
                ( _KeyDest == QKeyDest.Game    && ( !QConsole.ForcedUp || !_ConsoleKeys[key] ) ) )
            {
                string kb = _Bindings[key];
                if( !string.IsNullOrEmpty( kb ) )
                {
                    if( kb.StartsWith( "+" ) )
                    {
                        // button commands add keynum as a parm
                        QCommandBuffer.AddText( $"{kb} {key}\n" );
                    }
                    else
                    {
                        QCommandBuffer.AddText( kb );
                        QCommandBuffer.AddText( "\n" );
                    }
                }

                return;
            }

            if( !down )
                return; // other systems only care about key down events

            if( _ShiftDown )
            {
                key = _KeyShift[key];
            }

            switch( _KeyDest )
            {
                case QKeyDest.Message:
                    KeyMessage( key );
                    break;

                case QKeyDest.Menu:
                    menu.KeyDown( key );
                    break;

                case QKeyDest.Game:
                case QKeyDest.Console:
                    KeyConsole( key );
                    break;

                default:
                    sys.Error( "Bad key_dest" );
                    break;
            }
        }

        // Key_Init (void);
        public static void Init()
        {
            for( int i = 0; i < 32; i++ )
            {
                _Lines[i]    = new char[MAXCMDLINE];
                _Lines[i][0] = ']'; // key_lines[i][0] = ']'; key_lines[i][1] = 0;
            }

            LinePos = 1;

            //
            // init ascii characters in console mode
            //
            for( int i = 32; i < 128; i++ )
                _ConsoleKeys[i] = true;
            _ConsoleKeys[K_ENTER]      = true;
            _ConsoleKeys[K_TAB]        = true;
            _ConsoleKeys[K_LEFTARROW]  = true;
            _ConsoleKeys[K_RIGHTARROW] = true;
            _ConsoleKeys[K_UPARROW]    = true;
            _ConsoleKeys[K_DOWNARROW]  = true;
            _ConsoleKeys[K_BACKSPACE]  = true;
            _ConsoleKeys[K_PGUP]       = true;
            _ConsoleKeys[K_PGDN]       = true;
            _ConsoleKeys[K_SHIFT]      = true;
            _ConsoleKeys[K_MWHEELUP]   = true;
            _ConsoleKeys[K_MWHEELDOWN] = true;
            _ConsoleKeys['`']          = false;
            _ConsoleKeys['~']          = false;

            for( int i = 0; i < 256; i++ )
                _KeyShift[i] = i;
            for( int i = 'a'; i <= 'z'; i++ )
                _KeyShift[i] = i - 'a' + 'A';
            _KeyShift['1']  = '!';
            _KeyShift['2']  = '@';
            _KeyShift['3']  = '#';
            _KeyShift['4']  = '$';
            _KeyShift['5']  = '%';
            _KeyShift['6']  = '^';
            _KeyShift['7']  = '&';
            _KeyShift['8']  = '*';
            _KeyShift['9']  = '(';
            _KeyShift['0']  = ')';
            _KeyShift['-']  = '_';
            _KeyShift['=']  = '+';
            _KeyShift[',']  = '<';
            _KeyShift['.']  = '>';
            _KeyShift['/']  = '?';
            _KeyShift[';']  = ':';
            _KeyShift['\''] = '"';
            _KeyShift['[']  = '{';
            _KeyShift[']']  = '}';
            _KeyShift['`']  = '~';
            _KeyShift['\\'] = '|';

            _MenuBound[K_ESCAPE] = true;
            for( int i = 0; i < 12; i++ )
                _MenuBound[K_F1 + i] = true;

            //
            // register our functions
            //
            QCommand.Add( "bind",      Bind_f );
            QCommand.Add( "unbind",    Unbind_f );
            QCommand.Add( "unbindall", UnbindAll_f );
        }

        /// <summary>
        /// Key_WriteBindings
        /// </summary>
        public static void WriteBindings( Stream dest )
        {
            StringBuilder sb = new StringBuilder( 4096 );
            for( int i = 0; i < 256; i++ )
            {
                if( !string.IsNullOrEmpty( _Bindings[i] ) )
                {
                    sb.Append( "bind \"" );
                    sb.Append( KeynumToString( i ) );
                    sb.Append( "\" \"" );
                    sb.Append( _Bindings[i] );
                    sb.AppendLine( "\"" );
                }
            }

            byte[] buf = Encoding.ASCII.GetBytes( sb.ToString() );
            dest.Write( buf, 0, buf.Length );
        }

        /// <summary>
        /// Key_SetBinding
        /// </summary>
        public static void SetBinding( int keynum, string binding )
        {
            if( keynum != -1 )
            {
                _Bindings[keynum] = binding;
            }
        }

        // Key_ClearStates (void)
        public static void ClearStates()
        {
            for( int i = 0; i < 256; i++ )
            {
                _KeyDown[i] = false;
                _Repeats[i] = 0;
            }
        }

        // Key_KeynumToString
        //
        // Returns a string (either a single ascii char, or a K_* name) for the
        // given keynum.
        // $TODO: handle quote special (general escape sequence?)
        public static string KeynumToString( int keynum )
        {
            if( keynum == -1 )
                return "<KEY NOT FOUND>";

            if( keynum > 32 && keynum < 127 )
            {
                // printable ascii
                return ( (char) keynum ).ToString();
            }

            foreach( QKeyName kn in _KeyNames )
            {
                if( kn.keynum == keynum )
                    return kn.name;
            }

            return "<UNKNOWN KEYNUM>";
        }

        // Key_StringToKeynum
        //
        // Returns a key number to be used to index keybindings[] by looking at
        // the given string.  Single ascii characters return themselves, while
        // the K_* names are matched up.
        private static int StringToKeynum( string str )
        {
            if( string.IsNullOrEmpty( str ) )
                return -1;
            if( str.Length == 1 )
                return str[0];

            foreach( QKeyName keyname in _KeyNames )
            {
                if( QCommon.SameText( keyname.name, str ) )
                    return keyname.keynum;
            }

            return -1;
        }

        //Key_Unbind_f
        private static void Unbind_f()
        {
            if( QCommand.Argc != 2 )
            {
                QConsole.Print( "unbind <key> : remove commands from a key\n" );
                return;
            }

            int b = StringToKeynum( QCommand.Argv( 1 ) );
            if( b == -1 )
            {
                QConsole.Print( "\"{0}\" isn't a valid key\n", QCommand.Argv( 1 ) );
                return;
            }

            SetBinding( b, null );
        }

        // Key_Unbindall_f
        private static void UnbindAll_f()
        {
            for( int i = 0; i < 256; i++ )
                if( !string.IsNullOrEmpty( _Bindings[i] ) )
                    SetBinding( i, null );
        }

        //Key_Bind_f
        private static void Bind_f()
        {
            int c = QCommand.Argc;
            if( c != 2 && c != 3 )
            {
                QConsole.Print( "bind <key> [command] : attach a command to a key\n" );
                return;
            }

            int b = StringToKeynum( QCommand.Argv( 1 ) );
            if( b == -1 )
            {
                QConsole.Print( "\"{0}\" isn't a valid key\n", QCommand.Argv( 1 ) );
                return;
            }

            if( c == 2 )
            {
                if( !string.IsNullOrEmpty( _Bindings[b] ) ) // keybindings[b])
                    QConsole.Print( "\"{0}\" = \"{1}\"\n", QCommand.Argv( 1 ), _Bindings[b] );
                else
                    QConsole.Print( "\"{0}\" is not bound\n", QCommand.Argv( 1 ) );
                return;
            }

            // copy the rest of the command line
            // start out with a null string
            StringBuilder sb = new StringBuilder( 1024 );
            for( int i = 2; i < c; i++ )
            {
                if( i > 2 )
                    sb.Append( " " );
                sb.Append( QCommand.Argv( i ) );
            }

            SetBinding( b, sb.ToString() );
        }

        // Key_Message (int key)
        private static void KeyMessage( int key )
        {
            if( key == K_ENTER )
            {
                if( _TeamMessage )
                    QCommandBuffer.AddText( "say_team \"" );
                else
                    QCommandBuffer.AddText( "say \"" );
                QCommandBuffer.AddText( _ChatBuffer.ToString() );
                QCommandBuffer.AddText( "\"\n" );

                QKey.Destination   = QKeyDest.Game;
                _ChatBuffer.Length = 0;
                return;
            }

            if( key == K_ESCAPE )
            {
                QKey.Destination   = QKeyDest.Game;
                _ChatBuffer.Length = 0;
                return;
            }

            if( key < 32 || key > 127 )
                return; // non printable

            if( key == K_BACKSPACE )
            {
                if( _ChatBuffer.Length > 0 )
                {
                    _ChatBuffer.Length--;
                }

                return;
            }

            if( _ChatBuffer.Length == 31 )
                return; // all full

            _ChatBuffer.Append( (char) key );
        }

        /// <summary>
        /// Key_Console
        /// Interactive line editing and console scrollback
        /// </summary>
        private static void KeyConsole( int key )
        {
            if( key == K_ENTER )
            {
                string line = new string( _Lines[_EditLine] ).TrimEnd( '\0', ' ' );
                string cmd  = line.Substring( 1 );
                QCommandBuffer.AddText( cmd ); // skip the >
                QCommandBuffer.AddText( "\n" );
                QConsole.Print( "{0}\n", line );
                _EditLine            = ( _EditLine + 1 ) & 31;
                _HistoryLine         = _EditLine;
                _Lines[_EditLine][0] = ']';
                QKey.LinePos         = 1;
                if( QClient.cls.state == QServerType.DISCONNECTED )
                    Scr.UpdateScreen(); // force an update, because the command
                // may take some time
                return;
            }

            if( key == K_TAB )
            {
                // command completion
                string   txt   = new string( _Lines[_EditLine], 1, MAXCMDLINE - 1 ).TrimEnd( '\0', ' ' );
                string[] cmds  = QCommand.Complete( txt );
                string[] vars  = QCVar.CompleteName( txt );
                string   match = null;
                if( cmds != null )
                {
                    if( cmds.Length > 1 || vars != null )
                    {
                        QConsole.Print( "\nCommands:\n" );
                        foreach( string s in cmds )
                            QConsole.Print( "  {0}\n", s );
                    }
                    else
                        match = cmds[0];
                }

                if( vars != null )
                {
                    if( vars.Length > 1 || cmds != null )
                    {
                        QConsole.Print( "\nVariables:\n" );
                        foreach( string s in vars )
                            QConsole.Print( "  {0}\n", s );
                    }
                    else //if( match == null ) // ??? always true
                        match = vars[0];
                }

                if( !string.IsNullOrEmpty( match ) )
                {
                    int len = Math.Min( match.Length, MAXCMDLINE - 3 );
                    for( int i = 0; i < len; i++ )
                    {
                        _Lines[_EditLine][i + 1] = match[i];
                    }

                    QKey.LinePos                    = len + 1;
                    _Lines[_EditLine][QKey.LinePos] = ' ';
                    QKey.LinePos++;
                    _Lines[_EditLine][QKey.LinePos] = '\0';
                    return;
                }
            }

            if( key == K_BACKSPACE || key == K_LEFTARROW )
            {
                if( QKey.LinePos > 1 )
                    QKey.LinePos--;
                return;
            }

            if( key == K_UPARROW )
            {
                do
                {
                    _HistoryLine = ( _HistoryLine - 1 ) & 31;
                }
                while( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == 0 ) );

                if( _HistoryLine == _EditLine )
                    _HistoryLine = ( _EditLine + 1 ) & 31;
                Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], MAXCMDLINE );
                QKey.LinePos = 0;
                while( _Lines[_EditLine][QKey.LinePos] != '\0' && QKey.LinePos < MAXCMDLINE )
                    QKey.LinePos++;
                return;
            }

            if( key == K_DOWNARROW )
            {
                if( _HistoryLine == _EditLine )
                    return;
                do
                {
                    _HistoryLine = ( _HistoryLine + 1 ) & 31;
                }
                while( _HistoryLine != _EditLine && ( _Lines[_HistoryLine][1] == '\0' ) );

                if( _HistoryLine == _EditLine )
                {
                    _Lines[_EditLine][0] = ']';
                    QKey.LinePos         = 1;
                }
                else
                {
                    Array.Copy( _Lines[_HistoryLine], _Lines[_EditLine], MAXCMDLINE );
                    QKey.LinePos = 0;
                    while( _Lines[_EditLine][QKey.LinePos] != '\0' && QKey.LinePos < MAXCMDLINE )
                        QKey.LinePos++;
                }

                return;
            }

            if( key == K_PGUP || key == K_MWHEELUP )
            {
                QConsole.BackScroll += 2;
                if( QConsole.BackScroll > QConsole.TotalLines - ( Scr.vid.height >> 3 ) - 1 )
                    QConsole.BackScroll = QConsole.TotalLines - ( Scr.vid.height >> 3 ) - 1;
                return;
            }

            if( key == K_PGDN || key == K_MWHEELDOWN )
            {
                QConsole.BackScroll -= 2;
                if( QConsole.BackScroll < 0 )
                    QConsole.BackScroll = 0;
                return;
            }

            if( key == K_HOME )
            {
                QConsole.BackScroll = QConsole.TotalLines - ( Scr.vid.height >> 3 ) - 1;
                return;
            }

            if( key == K_END )
            {
                QConsole.BackScroll = 0;
                return;
            }

            if( key < 32 || key > 127 )
                return; // non printable

            if( QKey.LinePos < MAXCMDLINE - 1 )
            {
                _Lines[_EditLine][QKey.LinePos] = (char) key;
                QKey.LinePos++;
                _Lines[_EditLine][QKey.LinePos] = '\0';
            }
        }
    }

    // QKeyDest;
}
