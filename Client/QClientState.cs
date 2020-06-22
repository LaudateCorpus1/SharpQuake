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
    //
    // the QClientState structure is wiped completely at every
    // server signon
    //
    internal class QClientState
    {
        public int movemessages; // since connecting to this server

        // throw out the first couple, so the player
        // doesn't accidentally do something the
        // first frame
        public QUserCmd cmd; // last command sent to the server

        // information for local display
        public int[] stats; //[MAX_CL_STATS];	// health, etc

        public int     items;        // inventory bit flags
        public float[] item_gettime; //[32];	// cl.time of aquiring item, for blinking
        public float   faceanimtime; // use anim frame if cl.time < this

        public QColorShift[] cshifts;      //[NUM_CSHIFTS];	// color shifts for damage, powerups
        public QColorShift[] prev_cshifts; //[NUM_CSHIFTS];	// and content types

        // the QClient maintains its own idea of view angles, which are
        // sent to the server each frame.  The server sets punchangle when
        // the view is temporarliy offset, and an angle reset commands at the start
        // of each level and after teleporting.
        public Vector3[] mviewangles; //[2];	// during demo playback viewangles is lerped

        // between these
        public Vector3 viewangles;

        public Vector3[] mvelocity; //[2];	// update by server, used for lean+bob

        // (0 is newest)
        public Vector3 velocity; // lerped between mvelocity[0] and [1]

        public Vector3 punchangle; // temporary offset

        // pitch drifting vars
        public float idealpitch;

        public float  pitchvel;
        public bool   nodrift;
        public float  driftmove;
        public double laststop;

        public float viewheight;
        public float crouch; // local amount for smoothing stepups

        public bool paused; // send over by server
        public bool onground;
        public bool inwater;

        public int intermission;   // don't change view angle, full screen, etc
        public int completed_time; // latched at intermission start

        public double[] mtime; //[2];		// the timestamp of last two messages
        public double   time;  // clients view of time, should be between

        // servertime and oldservertime to generate
        // a lerp point for other data
        public double oldtime; // previous cl.time, time-oldtime is used

        // to decay light values and smooth step ups

        public float last_received_message; // (realtime) for net trouble icon

        //
        // information that is static for the entire time connected to a server
        //
        public model_t[] model_precache; // [MAX_MODELS];

        public QSoundFX[] sound_precache; // [MAX_SOUNDS];

        public string levelname;  // char[40];	// for display on solo scoreboard
        public int    viewentity; // cl_entitites[cl.viewentity] = player
        public int    maxclients;
        public int    gametype;

        // refresh related state
        public model_t worldmodel; // cl_entitites[0].model

        public efrag_t  free_efrags;  // first free efrag in list
        public int      num_entities; // held in cl_entities array
        public int      num_statics;  // held in cl_staticentities array
        public entity_t viewent;      // the gun model

        public int cdtrack, looptrack; // cd audio

        // frag scoreboard
        public QScoreboard[] scores; // [cl.maxclients]

        public bool HasItems( int item )
        {
            return ( this.items & item ) == item;
        }

        public void Clear()
        {
            this.movemessages = 0;
            this.cmd.Clear();
            Array.Clear( this.stats, 0, this.stats.Length );
            this.items = 0;
            Array.Clear( this.item_gettime, 0, this.item_gettime.Length );
            this.faceanimtime = 0;

            foreach( QColorShift cs in this.cshifts )
                cs.Clear();
            foreach( QColorShift cs in this.prev_cshifts )
                cs.Clear();

            this.mviewangles[0] = Vector3.Zero;
            this.mviewangles[1] = Vector3.Zero;
            this.viewangles     = Vector3.Zero;
            this.mvelocity[0]   = Vector3.Zero;
            this.mvelocity[1]   = Vector3.Zero;
            this.velocity       = Vector3.Zero;
            this.punchangle     = Vector3.Zero;

            this.idealpitch = 0;
            this.pitchvel   = 0;
            this.nodrift    = false;
            this.driftmove  = 0;
            this.laststop   = 0;

            this.viewheight = 0;
            this.crouch     = 0;

            this.paused   = false;
            this.onground = false;
            this.inwater  = false;

            this.intermission   = 0;
            this.completed_time = 0;

            this.mtime[0]              = 0;
            this.mtime[1]              = 0;
            this.time                  = 0;
            this.oldtime               = 0;
            this.last_received_message = 0;

            Array.Clear( this.model_precache, 0, this.model_precache.Length );
            Array.Clear( this.sound_precache, 0, this.sound_precache.Length );

            this.levelname  = null;
            this.viewentity = 0;
            this.maxclients = 0;
            this.gametype   = 0;

            this.worldmodel   = null;
            this.free_efrags  = null;
            this.num_entities = 0;
            this.num_statics  = 0;
            this.viewent.Clear();

            this.cdtrack   = 0;
            this.looptrack = 0;

            this.scores = null;
        }

        public QClientState()
        {
            this.stats        = new int[QStats.MAX_CL_STATS];
            this.item_gettime = new float[32]; // ???????????

            this.cshifts = new QColorShift[QViewFlashFlag.NUM_CSHIFTS];
            for( int i = 0; i < QViewFlashFlag.NUM_CSHIFTS; i++ )
                this.cshifts[i] = new QColorShift();

            this.prev_cshifts = new QColorShift[QViewFlashFlag.NUM_CSHIFTS];
            for( int i = 0; i < QViewFlashFlag.NUM_CSHIFTS; i++ )
                this.prev_cshifts[i] = new QColorShift();

            this.mviewangles    = new Vector3[2]; //??????
            this.mvelocity      = new Vector3[2];
            this.mtime          = new double[2];
            this.model_precache = new model_t[QDef.MAX_MODELS];
            this.sound_precache = new QSoundFX[QDef.MAX_SOUNDS];
            this.viewent        = new entity_t();
        }
    }
}
