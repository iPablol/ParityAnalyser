
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System;
using ParityAnalyserCore;
namespace ParityAnalyserCore.Sim
{
    public class Simulation
    {
		public static Vector2 gridCenter = new (1.5f, 1f);

		public static readonly float bombRadius = 0.48f;
        public static readonly float sliderThreshold = 1 / 11.5f; // Thanks yabje for those 1/12 sliders


        public static readonly float minSaberX = -0.2f;
        public static readonly float maxSaberX = 3.2f;

        public static readonly float minSaberY = -0.2f;
        public static readonly float maxSaberY = 2.2f;

        public Simulation(List<BaseNote> notes, ParityAnalyser.Options? options = null, IDebugRenderer? debugRenderer = null, Action<string>? debugCallback = null)
        {
            ParityAnalyser.options = options ?? ParityAnalyser.Options.Default;
            DebugRenderer.renderer = debugRenderer;
            ParityAnalyser.debugCallback = debugCallback ?? Console.WriteLine;
            // TODO: Check starting parity
            if (notes.Any(note => note.type == NoteType.Red))
            {
                this.leftSaber = new LeftSaber((from note in notes
                                               where note.type == NoteType.Red || note.type == NoteType.Bomb
                                               orderby note.JsonTime ascending,
                                                       note.type == NoteType.Bomb ? 0 : 1
                                               select note).ToList());
            }
            if (notes.Any(note => note.type == NoteType.Blue))
            {
                this.rightSaber = new RightSaber((from note in notes
                                              where note.type == NoteType.Blue || note.type == NoteType.Bomb
                                              orderby note.JsonTime ascending,
                                                      note.type == NoteType.Bomb ? 0 : 1
                                              select note).ToList());
            }
        }

        public void Run()
        {
            ParityAnalyser.Log($"Bomb cluster merging is: {(ParityAnalyser.options.bombClusterMerging ? "ON" : "OFF")}");
            ParityAnalyser.Log("-------------- Left Saber --------------");
            if (leftSaber != null)
            {
				if (ParityAnalyser.options.renderLeftBombGroups)
					leftSaber.RenderBombGroups(true, true);

				foreach (SaberSnapshot s in this.leftSaber.FirstSwing())
                {
                    redParities.Add(s);
                }
                foreach ((ISimulationObject note1, ISimulationObject note2) in this.leftSaber.GetPairs())
                {
                    //Debug.Log($"({note1.Time()} - {note2.Time()}) Prev: {note1.GetType()}, Next: {note2.GetType()}");
                    //Debug.Log("");
                    foreach (SaberSnapshot snap in this.leftSaber.Swing(note1, note2))
                    {
                        redParities.Add(snap);
                    }
                }

            }
            ParityAnalyser.Log("----------------------------------------\n");
			ParityAnalyser.Log("-------------- Right Saber --------------");
            if (rightSaber != null)
            {
				if (ParityAnalyser.options.renderRightBombGroups)
					rightSaber.RenderBombGroups(true, true);
				foreach (SaberSnapshot s in this.rightSaber.FirstSwing())
                {
                    blueParities.Add(s);
                }
                foreach ((ISimulationObject note1, ISimulationObject note2) in this.rightSaber.GetPairs())
                {
                    //Debug.Log($"({note1.Time()} - {note2.Time()}) Prev: {note1.GetType()}, Next: {note2.GetType()}");
                    //Debug.Log("");
                    foreach (SaberSnapshot snap in this.rightSaber.Swing(note1, note2))
                    {
                        blueParities.Add(snap);
                    }
                }

                //foreach (SaberSnapshot snap in blueParities)
                //{
                //    Debug.Log(snap);
                //}

            }
			ParityAnalyser.Log("-----------------------------------------\n");
        }

        private RightSaber rightSaber = null;
        private LeftSaber leftSaber = null;

        public List<SaberSnapshot> blueParities { get; private set; } = [];
        public List<SaberSnapshot> redParities { get; private set; } = [];

        
    }

    public record struct SaberSnapshot(BaseNote note, Vector3 position, Parity parity, AngleRange wristAngle, bool reset = false)
    {

        public Vector3 hilt => position;
        public Vector3 tip => position + (Saber.length * Vector3.Transform(Vector3.up, rotation));
        public Quaternion rotation =>  Quaternion.CreateFromAxisAngle(Vector3.forward, Math.Deg2Rad * wristAngle) * Quaternion.CreateFromAxisAngle(Vector3.right, !parity.Bool() ? 0 : -(float)Math.PI);


        public float beat => note.JsonTime;

        public override string ToString() => $"Beat: {beat}, CutDir: {note.cutDirection}, Position: {position}, Wrist Angle: {wristAngle}, Parity: {parity.ToString()}{(reset ? ", Reset" : "")}";
    }
}
