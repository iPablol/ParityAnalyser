
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

using ParityAnalyserCore.Sim;

namespace ParityAnalyserCore
{
    public class ParityAnalyser
    {

        public static Options options = new();

        public record struct Options()
        {
			public bool renderLeftParitySabers = true;
			public bool renderRightParitySabers = true;

			public bool renderLeftParityOutlines = true;
			public bool renderRightParityOutlines = true;

			public bool animateLeftParities = true;
			public bool animateRightParities = true;

			public bool renderLeftBombGroups = false;
			public bool renderRightBombGroups = false;

			public bool debugLeftBombCollisions = false;
			public bool renderLeftBombDodgePoints = false;
			public bool debugRightBombCollisions = false;
			public bool renderRightBombDodgePoints = false;

			public bool logResets = true;
			public bool debugDotState = false;
			public bool debugDotAngle = false;
			public bool bombClusterMerging = true;
		}
    }


    // If in a snapshot, represents the parity of that hit, if in the saber it represents the parity of the next hit
    public enum Parity
    {
        FOREHAND = -1,
        BACKHAND = 1
    }
}
