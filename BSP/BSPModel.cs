﻿/* Rewritten in C# by Yury Kiselev, 2010.
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

using System.Runtime.InteropServices;

namespace SharpQuake
{
    [StructLayout( LayoutKind.Sequential, Pack = 1 )]
    internal struct BSPModel
    {
        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public float[] mins; // [3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public float[] maxs; //[3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 3 )]
        public float[] origin; // [3];

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = BSPFile.MAX_MAP_HULLS )]
        public int[] headnode; //[MAX_MAP_HULLS];

        public int visleafs; // not including the solid leaf 0
        public int firstface, numfaces;

        public static int SizeInBytes = Marshal.SizeOf( typeof( BSPModel ) );
    }
}