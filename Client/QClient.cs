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
    static partial class QClient
    {
        public static QStaticClient cls      => _Static;
        public static QClientState  cl       => _State;
        public static entity_t[]    Entities => _Entities;

        /// <summary>
        /// cl_entities[cl.viewentity]
        /// Player model (visible when out of body)
        /// </summary>
        public static entity_t ViewEntity => _Entities[_State.viewentity];

        /// <summary>
        /// cl.viewent
        /// Weapon model (only visible from inside body)
        /// </summary>
        public static entity_t ViewEnt => _State.viewent;

        public static float           ForwardSpeed => _ForwardSpeed.Value;
        public static bool            LookSpring   => ( Math.Abs( _LookSpring.Value ) > 0.001f );
        public static bool            LookStrafe   => ( Math.Abs( _LookStrafe.Value ) > 0.001f );
        public static QDynamicLight[] DLights      => _DLights;
        public static QLightStyle[]   LightStyle   => _LightStyle;
        public static entity_t[]      VisEdicts    => _VisEdicts;
        public static float           Sensitivity  => _Sensitivity.Value;
        public static float           MSide        => _MSide.Value;
        public static float           MYaw         => _MYaw.Value;
        public static float           MPitch       => _MPitch.Value;
        public static float           MForward     => _MForward.Value;
        public static string          Name         => _Name.String;
        public static float           Color        => _Color.Value;

        public const int SIGNONS       = 4; // signon messages to receive before connected
        public const int MAX_DLIGHTS   = 32;
        public const int MAX_BEAMS     = 24;
        public const int MAX_EFRAGS    = 640;
        public const int MAX_MAPSTRING = 2048;
        public const int MAX_DEMOS     = 8;
        public const int MAX_DEMONAME  = 16;
        public const int MAX_VISEDICTS = 256;

        public static int NumVisEdicts;
        private const int MAX_TEMP_ENTITIES   = 64;  // lightning bolts, etc
        private const int MAX_STATIC_ENTITIES = 128; // torches, etc

        private static QStaticClient _Static = new QStaticClient();
        private static QClientState  _State  = new QClientState();

        private static efrag_t[]       _EFrags         = new efrag_t[MAX_EFRAGS];               // cl_efrags
        private static entity_t[]      _Entities       = new entity_t[QDef.MAX_EDICTS];         // cl_entities
        private static entity_t[]      _StaticEntities = new entity_t[MAX_STATIC_ENTITIES];     // cl_static_entities
        private static QLightStyle[]   _LightStyle     = new QLightStyle[QDef.MAX_LIGHTSTYLES]; // cl_lightstyle
        private static QDynamicLight[] _DLights        = new QDynamicLight[MAX_DLIGHTS];        // cl_dlights

        private static QCVar _Name;          // = { "_cl_name", "player", true };
        private static QCVar _Color;         // = { "_cl_color", "0", true };
        private static QCVar _ShowNet;       // = { "cl_shownet", "0" };	// can be 0, 1, or 2
        private static QCVar _NoLerp;        // = { "cl_nolerp", "0" };
        private static QCVar _LookSpring;    // = { "lookspring", "0", true };
        private static QCVar _LookStrafe;    // = { "lookstrafe", "0", true };
        private static QCVar _Sensitivity;   // = { "sensitivity", "3", true };
        private static QCVar _MPitch;        // = { "m_pitch", "0.022", true };
        private static QCVar _MYaw;          // = { "m_yaw", "0.022", true };
        private static QCVar _MForward;      // = { "m_forward", "1", true };
        private static QCVar _MSide;         // = { "m_side", "0.8", true };
        private static QCVar _UpSpeed;       // = { "cl_upspeed", "200" };
        private static QCVar _ForwardSpeed;  // = { "cl_forwardspeed", "200", true };
        private static QCVar _BackSpeed;     // = { "cl_backspeed", "200", true };
        private static QCVar _SideSpeed;     // = { "cl_sidespeed", "350" };
        private static QCVar _MoveSpeedKey;  // = { "cl_movespeedkey", "2.0" };
        private static QCVar _YawSpeed;      // = { "cl_yawspeed", "140" };
        private static QCVar _PitchSpeed;    // = { "cl_pitchspeed", "150" };
        private static QCVar _AngleSpeedKey; // = { "cl_anglespeedkey", "1.5" };

        private static entity_t[] _VisEdicts = new entity_t[MAX_VISEDICTS]; // cl_visedicts[MAX_VISEDICTS]
    }
}
