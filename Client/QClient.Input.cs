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

// cl_input.c

namespace SharpQuake
{
    partial class QClient
    {
        // CL_SendMove
        public static void SendMove( ref QUserCmd cmd )
        {
            cl.cmd = cmd; // cl.QCommand = *QCommand - struct copying!!!

            QMessageWriter MessageWriter = new QMessageWriter( 128 );

            //
            // send the movement message
            //
            MessageWriter.WriteByte( protocol.clc_move );

            MessageWriter.WriteFloat( (float)cl.mtime[0] );	// so server can get ping times

            MessageWriter.WriteAngle( cl.viewangles.X );
            MessageWriter.WriteAngle( cl.viewangles.Y );
            MessageWriter.WriteAngle( cl.viewangles.Z );

            MessageWriter.WriteShort( (short)cmd.forwardmove );
            MessageWriter.WriteShort( (short)cmd.sidemove );
            MessageWriter.WriteShort( (short)cmd.upmove );

            //
            // send button bits
            //
            int bits = 0;

            if( ( QClientInput.AttackBtn.state & 3 ) != 0 )
                bits |= 1;
            QClientInput.AttackBtn.state &= ~2;

            if( ( QClientInput.JumpBtn.state & 3 ) != 0 )
                bits |= 2;
            QClientInput.JumpBtn.state &= ~2;

            MessageWriter.WriteByte( bits );

            MessageWriter.WriteByte( QClientInput.Impulse );
            QClientInput.Impulse = 0;

            //
            // deliver the message
            //
            if( cls.demoplayback )
                return;

            //
            // allways dump the first two message, because it may contain leftover inputs
            // from the last level
            //
            if( ++cl.movemessages <= 2 )
                return;

            if( net.SendUnreliableMessage( cls.netcon, MessageWriter ) == -1 )
            {
                Con.Print( "CL_SendMove: lost server connection\n" );
                Disconnect();
            }
        }

        // CL_InitInput
        private static void InitInput()
        {
            QClientInput.Init();
        }

        /// <summary>
        /// CL_BaseMove
        /// Send the intended movement message to the server
        /// </summary>
        private static void BaseMove( ref QUserCmd cmd )
        {
            if( cls.signon != SIGNONS )
                return;

            AdjustAngles();

            cmd.Clear();

            if( QClientInput.StrafeBtn.IsDown )
            {
                cmd.sidemove += _SideSpeed.Value * KeyState( ref QClientInput.RightBtn );
                cmd.sidemove -= _SideSpeed.Value * KeyState( ref QClientInput.LeftBtn );
            }

            cmd.sidemove += _SideSpeed.Value * KeyState( ref QClientInput.MoveRightBtn );
            cmd.sidemove -= _SideSpeed.Value * KeyState( ref QClientInput.MoveLeftBtn );

            cmd.upmove += _UpSpeed.Value * KeyState( ref QClientInput.UpBtn );
            cmd.upmove -= _UpSpeed.Value * KeyState( ref QClientInput.DownBtn );

            if( !QClientInput.KLookBtn.IsDown )
            {
                cmd.forwardmove += _ForwardSpeed.Value * KeyState( ref QClientInput.ForwardBtn );
                cmd.forwardmove -= _BackSpeed.Value * KeyState( ref QClientInput.BackBtn );
            }

            //
            // adjust for speed key
            //
            if( QClientInput.SpeedBtn.IsDown )
            {
                cmd.forwardmove *= _MoveSpeedKey.Value;
                cmd.sidemove *= _MoveSpeedKey.Value;
                cmd.upmove *= _MoveSpeedKey.Value;
            }
        }

        // CL_AdjustAngles
        //
        // Moves the local angle positions
        private static void AdjustAngles()
        {
            float speed = (float)host.FrameTime;

            if( QClientInput.SpeedBtn.IsDown )
                speed *= _AngleSpeedKey.Value;

            if( !QClientInput.StrafeBtn.IsDown )
            {
                cl.viewangles.Y -= speed * _YawSpeed.Value * KeyState( ref QClientInput.RightBtn );
                cl.viewangles.Y += speed * _YawSpeed.Value * KeyState( ref QClientInput.LeftBtn );
                cl.viewangles.Y = mathlib.AngleMod( cl.viewangles.Y );
            }

            if( QClientInput.KLookBtn.IsDown )
            {
                view.StopPitchDrift();
                cl.viewangles.X -= speed * _PitchSpeed.Value * KeyState( ref QClientInput.ForwardBtn );
                cl.viewangles.X += speed * _PitchSpeed.Value * KeyState( ref QClientInput.BackBtn );
            }

            float up = KeyState( ref QClientInput.LookUpBtn );
            float down = KeyState( ref QClientInput.LookDownBtn );

            cl.viewangles.X -= speed * _PitchSpeed.Value * up;
            cl.viewangles.X += speed * _PitchSpeed.Value * down;

            if( Math.Abs( up ) > 0.001f || Math.Abs( down ) > 0.001f )
                view.StopPitchDrift();

            if( cl.viewangles.X > 80 )
                cl.viewangles.X = 80;
            if( cl.viewangles.X < -70 )
                cl.viewangles.X = -70;

            if( cl.viewangles.Z > 50 )
                cl.viewangles.Z = 50;
            if( cl.viewangles.Z < -50 )
                cl.viewangles.Z = -50;
        }

        // CL_KeyState
        //
        // Returns 0.25 if a key was pressed and released during the frame,
        // 0.5 if it was pressed and held
        // 0 if held then released, and
        // 1.0 if held for the entire time
        private static float KeyState( ref QButton key )
        {
            bool impulsedown = ( key.state & 2 ) != 0;
            bool impulseup = ( key.state & 4 ) != 0;
            bool down = key.IsDown;// ->state & 1;
            float val = 0;

            if( impulsedown && !impulseup )
                if( down )
                    val = 0.5f;	// pressed and held this frame
                else
                    val = 0;	//	I_Error ();
            if( impulseup && !impulsedown )
                if( down )
                    val = 0;	//	I_Error ();
                else
                    val = 0;	// released this frame
            if( !impulsedown && !impulseup )
                if( down )
                    val = 1.0f;	// held the entire frame
                else
                    val = 0;	// up the entire frame
            if( impulsedown && impulseup )
                if( down )
                    val = 0.75f;	// released and re-pressed this frame
                else
                    val = 0.25f;	// pressed and released this frame

            key.state &= 1;		// clear impulses

            return val;
        }
    }
}
