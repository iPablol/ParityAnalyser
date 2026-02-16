
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

using ParityAnalyser.Sim;


namespace ParityAnalyser
{
    public class ParityAnalyser
    {

        public static Options options = new();

        public record struct Options()
        {
            public bool bombClusterMerging = true;
            public bool debugLeftBombCollisions = false;
            public bool debugRightBombCollisions = false;
            public bool debugDotState = false;
		}
    }


    // If in a snapshot, represents the parity of that hit, if in the saber it represents the parity of the next hit
    public enum Parity
    {
        FOREHAND = -1,
        BACKHAND = 1
    }
}
