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
    internal static partial class QCommon
    {
        public static bool SameText( string a, string b )
        {
            return ( string.Compare( a, b, true ) == 0 );
        }

        public static bool SameText( string a, string b, int count )
        {
            return ( string.Compare( a, 0, b, 0, count, true ) == 0 );
        }

        public static string Copy( string src, int maxLength )
        {
            if( src == null )
                return null;

            return ( src.Length > maxLength ? src.Substring( 1, maxLength ) : src );
        }

        public static string GetString( byte[] src )
        {
            int count = 0;
            while( count < src.Length && src[count] != 0 )
                count++;

            return ( count > 0 ? Encoding.ASCII.GetString( src, 0, count ) : string.Empty );
        }
    }
}
