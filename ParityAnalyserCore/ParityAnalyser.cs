
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System;

using ParityAnalyserCore.Sim;

namespace ParityAnalyserCore
{
    public class ParityAnalyser
    {
		public static Action<string>? debugCallback;
		public static Options options = new();

		public static void Log(string message) => debugCallback?.Invoke(message);

        public record struct Options()
        {

			public bool renderLeftBombGroups = false;
			public bool renderRightBombGroups = false;

			public bool debugLeftBombCollisions = false;
			public bool renderLeftBombDodgePoints = false;
			public bool debugRightBombCollisions = false;
			public bool renderRightBombDodgePoints = false;

			public bool logResets = true;
			public bool debugDotState = false;
			public bool bombClusterMerging = true;

			public static Options Default = new();
		}
    }


    // If in a snapshot, represents the parity of that hit, if in the saber it represents the parity of the next hit
    public enum Parity
    {
        FOREHAND = -1,
        BACKHAND = 1
    }
}
