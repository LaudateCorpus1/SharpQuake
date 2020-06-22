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
    internal class QColorShift
    {
        public int[] destcolor; // [3];
        public int   percent;   // 0-256

        public void Clear()
        {
            this.destcolor[0] = 0;
            this.destcolor[1] = 0;
            this.destcolor[2] = 0;
            this.percent      = 0;
        }

        public QColorShift()
        {
            destcolor = new int[3];
        }

        public QColorShift( int[] destColor, int percent )
        {
            if( destColor.Length != 3 )
            {
                throw new ArgumentException( "destColor must have length of 3 elements!" );
            }

            this.destcolor = destColor;
            this.percent   = percent;
        }
    }
}
