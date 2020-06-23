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
    internal class QDisposableWrapper<T> : IDisposable where T : class, IDisposable
    {
        public T Object => _Object;

        private T    _Object;
        private bool _Owned;

        private void Dispose( bool disposing )
        {
            if( _Object != null && _Owned )
            {
                _Object.Dispose();
                _Object = null;
            }
        }

        public QDisposableWrapper( T obj, bool dispose )
        {
            _Object = obj;
            _Owned  = dispose;
        }

        ~QDisposableWrapper()
        {
            Dispose( false );
        }

        public void Dispose()
        {
            Dispose( true );
            GC.SuppressFinalize( this );
        }
    }
}
