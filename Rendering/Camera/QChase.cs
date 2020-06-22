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
using OpenTK;

namespace SharpQuake
{
    /// <summary>
    /// Chase_functions
    /// </summary>
    internal static class QChase
    {
        /// <summary>
        /// chase_active.value != 0
        /// </summary>
        public static bool IsActive => ( Math.Abs( _Active.Value ) > 0.001f ); // fix code improvement warning for possible floating point precision problems

        private static cvar _Back;// = { "chase_back", "100" };
        private static cvar _Up;// = { "chase_up", "16" };
        private static cvar _Right;// = { "chase_right", "0" };
        private static cvar _Active;// = { "chase_active", "0" };
        private static Vector3 _Dest;

        // Chase_Init
        public static void Init()
        {
            if( _Back == null )
            {
                _Back = new cvar( "chase_back", "100" );
                _Up = new cvar( "chase_up", "16" );
                _Right = new cvar( "chase_right", "0" );
                _Active = new cvar( "chase_active", "0" );
            }
        }

        // Chase_Reset
        public static void Reset()
        {
            // for respawning and teleporting
            //	start position 12 units behind head
        }

        // Chase_Update
        public static void Update()
        {
            // if can't see player, reset
            mathlib.AngleVectors( ref QClient.cl.viewangles, out Vector3 forward, out Vector3 right, out Vector3 up );

            // calc exact destination
            _Dest = render.RefDef.vieworg - forward * _Back.Value - right * _Right.Value;
            _Dest.Z = render.RefDef.vieworg.Z + _Up.Value;

            // find the spot the player is looking at
            Vector3 dest = render.RefDef.vieworg + forward * 4096;

            TraceLine( ref render.RefDef.vieworg, ref dest, out Vector3 stop );

            // calculate pitch to look at the same spot from camera
            stop -= render.RefDef.vieworg;
            Vector3.Dot( ref stop, ref forward, out float dist );
            if( dist < 1 )
                dist = 1;

            render.RefDef.viewangles.X = (float)( -Math.Atan( stop.Z / dist ) / Math.PI * 180.0 );
            //r_refdef.viewangles[PITCH] = -atan(stop[2] / dist) / M_PI * 180;

            // move towards destination
            render.RefDef.vieworg = _Dest; //VectorCopy(chase_dest, r_refdef.vieworg);
        }

        private static void TraceLine( ref Vector3 start, ref Vector3 end, out Vector3 impact )
        {
            trace_t trace = new trace_t();

            server.RecursiveHullCheck( QClient.cl.worldmodel.hulls[0], 0, 0, 1, ref start, ref end, trace );

            impact = trace.endpos; // VectorCopy(trace.endpos, impact);
        }
    }
}
