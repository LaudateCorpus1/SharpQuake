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

namespace SharpQuake
{
    internal static class QClientInput
    {
        // QButton in_xxx
        public static QButton MLookBtn;

        public static QButton KLookBtn;
        public static QButton LeftBtn;
        public static QButton RightBtn;
        public static QButton ForwardBtn;
        public static QButton BackBtn;
        public static QButton LookUpBtn;
        public static QButton LookDownBtn;
        public static QButton MoveLeftBtn;
        public static QButton MoveRightBtn;
        public static QButton StrafeBtn;
        public static QButton SpeedBtn;
        public static QButton UseBtn;
        public static QButton JumpBtn;
        public static QButton AttackBtn;
        public static QButton UpBtn;
        public static QButton DownBtn;

        public static int Impulse;

        public static void Init()
        {
            QCommand.Add( "+moveup",    UpDown );
            QCommand.Add( "-moveup",    UpUp );
            QCommand.Add( "+movedown",  DownDown );
            QCommand.Add( "-movedown",  DownUp );
            QCommand.Add( "+left",      LeftDown );
            QCommand.Add( "-left",      LeftUp );
            QCommand.Add( "+right",     RightDown );
            QCommand.Add( "-right",     RightUp );
            QCommand.Add( "+forward",   ForwardDown );
            QCommand.Add( "-forward",   ForwardUp );
            QCommand.Add( "+back",      BackDown );
            QCommand.Add( "-back",      BackUp );
            QCommand.Add( "+lookup",    LookupDown );
            QCommand.Add( "-lookup",    LookupUp );
            QCommand.Add( "+lookdown",  LookdownDown );
            QCommand.Add( "-lookdown",  LookdownUp );
            QCommand.Add( "+strafe",    StrafeDown );
            QCommand.Add( "-strafe",    StrafeUp );
            QCommand.Add( "+moveleft",  MoveleftDown );
            QCommand.Add( "-moveleft",  MoveleftUp );
            QCommand.Add( "+moveright", MoverightDown );
            QCommand.Add( "-moveright", MoverightUp );
            QCommand.Add( "+speed",     SpeedDown );
            QCommand.Add( "-speed",     SpeedUp );
            QCommand.Add( "+attack",    AttackDown );
            QCommand.Add( "-attack",    AttackUp );
            QCommand.Add( "+use",       UseDown );
            QCommand.Add( "-use",       UseUp );
            QCommand.Add( "+jump",      JumpDown );
            QCommand.Add( "-jump",      JumpUp );
            QCommand.Add( "impulse",    ImpulseCmd );
            QCommand.Add( "+klook",     KLookDown );
            QCommand.Add( "-klook",     KLookUp );
            QCommand.Add( "+mlook",     MLookDown );
            QCommand.Add( "-mlook",     MLookUp );
        }

        private static void KeyDown( ref QButton b )
        {
            int    k;
            string c = QCommand.Argv( 1 );
            if( !string.IsNullOrEmpty( c ) )
                k = int.Parse( c );
            else
                k = -1; // typed manually at the console for continuous down

            if( k == b.down0 || k == b.down1 )
                return; // repeating key

            if( b.down0 == 0 )
                b.down0 = k;
            else if( b.down1 == 0 )
                b.down1 = k;
            else
            {
                Con.Print( "Three keys down for a button!\n" );
                return;
            }

            if( ( b.state & 1 ) != 0 )
                return;       // still down
            b.state |= 1 + 2; // down + impulse down
        }

        private static void KeyUp( ref QButton b )
        {
            int    k;
            string c = QCommand.Argv( 1 );
            if( !string.IsNullOrEmpty( c ) )
                k = int.Parse( c );
            else
            {
                // typed manually at the console, assume for unsticking, so clear all
                b.down0 = b.down1 = 0;
                b.state = 4; // impulse up
                return;
            }

            if( b.down0 == k )
                b.down0 = 0;
            else if( b.down1 == k )
                b.down1 = 0;
            else
                return; // key up without coresponding down (menu pass through)

            if( b.down0 != 0 || b.down1 != 0 )
                return; // some other key is still holding it down

            if( ( b.state & 1 ) == 0 )
                return;    // still up (this should not happen)
            b.state &= ~1; // now up
            b.state |= 4;  // impulse up
        }

        private static void KLookDown()
        {
            KeyDown( ref KLookBtn );
        }

        private static void KLookUp()
        {
            KeyUp( ref KLookBtn );
        }

        private static void MLookDown()
        {
            KeyDown( ref MLookBtn );
        }

        private static void MLookUp()
        {
            KeyUp( ref MLookBtn );

            if( ( MLookBtn.state & 1 ) == 0 && QClient.LookSpring )
                view.StartPitchDrift();
        }

        private static void UpDown()
        {
            KeyDown( ref UpBtn );
        }

        private static void UpUp()
        {
            KeyUp( ref UpBtn );
        }

        private static void DownDown()
        {
            KeyDown( ref DownBtn );
        }

        private static void DownUp()
        {
            KeyUp( ref DownBtn );
        }

        private static void LeftDown()
        {
            KeyDown( ref LeftBtn );
        }

        private static void LeftUp()
        {
            KeyUp( ref LeftBtn );
        }

        private static void RightDown()
        {
            KeyDown( ref RightBtn );
        }

        private static void RightUp()
        {
            KeyUp( ref RightBtn );
        }

        private static void ForwardDown()
        {
            KeyDown( ref ForwardBtn );
        }

        private static void ForwardUp()
        {
            KeyUp( ref ForwardBtn );
        }

        private static void BackDown()
        {
            KeyDown( ref BackBtn );
        }

        private static void BackUp()
        {
            KeyUp( ref BackBtn );
        }

        private static void LookupDown()
        {
            KeyDown( ref LookUpBtn );
        }

        private static void LookupUp()
        {
            KeyUp( ref LookUpBtn );
        }

        private static void LookdownDown()
        {
            KeyDown( ref LookDownBtn );
        }

        private static void LookdownUp()
        {
            KeyUp( ref LookDownBtn );
        }

        private static void MoveleftDown()
        {
            KeyDown( ref MoveLeftBtn );
        }

        private static void MoveleftUp()
        {
            KeyUp( ref MoveLeftBtn );
        }

        private static void MoverightDown()
        {
            KeyDown( ref MoveRightBtn );
        }

        private static void MoverightUp()
        {
            KeyUp( ref MoveRightBtn );
        }

        private static void SpeedDown()
        {
            KeyDown( ref SpeedBtn );
        }

        private static void SpeedUp()
        {
            KeyUp( ref SpeedBtn );
        }

        private static void StrafeDown()
        {
            KeyDown( ref StrafeBtn );
        }

        private static void StrafeUp()
        {
            KeyUp( ref StrafeBtn );
        }

        private static void AttackDown()
        {
            KeyDown( ref AttackBtn );
        }

        private static void AttackUp()
        {
            KeyUp( ref AttackBtn );
        }

        private static void UseDown()
        {
            KeyDown( ref UseBtn );
        }

        private static void UseUp()
        {
            KeyUp( ref UseBtn );
        }

        private static void JumpDown()
        {
            KeyDown( ref JumpBtn );
        }

        private static void JumpUp()
        {
            KeyUp( ref JumpBtn );
        }

        private static void ImpulseCmd()
        {
            Impulse = QCommon.atoi( QCommand.Argv( 1 ) );
        }
    }
}
