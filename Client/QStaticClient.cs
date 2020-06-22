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
    // the QStaticClient structure is persistant through an arbitrary number
    // of server connection
    internal class QStaticClient
    {
        public ServerType state;

        // personalization data sent to server
        public string mapstring;
        public string spawnparms; // to restart a level

        // demo loop control
        public int      demonum; // -1 = don't play demos
        public string[] demos;   // when not playing

        // demo recording info must be here, because record is started before
        // entering a map (and clearing QClientState)
        public bool demorecording;

        public bool        demoplayback;
        public bool        timedemo;
        public int         forcetrack;    // -1 = use normal cd track
        public IDisposable demofile;      // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
        public int         td_lastframe;  // to meter out one message a frame
        public int         td_startframe; // host_framecount at start
        public float       td_starttime;  // realtime at second frame of timedemo

        // connection information
        public int signon; // 0 to SIGNONS

        public qsocket_t netcon;  // qsocket_t	*netcon;
        public MsgWriter message; // sizebuf_t	message;		// writing buffer to send to server

        public QStaticClient()
        {
            this.demos   = new string[QClient.MAX_DEMOS];
            this.message = new MsgWriter( 1024 ); // like in Client_Init()
        }
    }
}
