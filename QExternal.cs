using System;
using System.Runtime.InteropServices;

namespace SharpQuake
{

    internal class QExternal
    {
#if _WINDOWS

        internal class WindowsShell
        {
            /// <summary>
            /// <para>
            /// Notifies the system of an event that an application has performed.
            /// </para>
            /// An application should use this function if it performs an action that may affect the Shell.
            /// <para>
            /// </para>
            /// </summary>
            /// <param name="eventId"></param>
            /// <param name="flags"></param>
            /// <param name="item1"></param>
            /// <param name="item2"></param>
            /// <returns></returns>
            [DllImport( "Shell32.dll" )]
            public static extern int SHChangeNotify( int eventId, int flags, IntPtr item1, IntPtr item2 );
        }

#endif
    }
}
