using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;
using Parity = ParityAnalyser.ParityAnalyser.Parity;

namespace ParityAnalyser.Sim
{
    public class Simulation
    {
        public static readonly float bombRadius = 0.75f;
        public static readonly float sliderThreshold = 1 / 11.5f; // Thanks yabje for those 1/12 sliders
        public Simulation(List<BaseNote> notes)
        {
            // TODO: Check starting parity
            this.leftSaber = new LeftSaber((from note in notes
                                           where note.Type == (int)NoteType.Red || note.Type == (int)NoteType.Bomb
                                           orderby note.JsonTime ascending,
                                                   note.Type == (int)NoteType.Bomb ? 0 : 1
                                           select note).ToList());
            this.rightSaber = new RightSaber((from note in notes
                                              where note.Type == (int)NoteType.Blue || note.Type == (int)NoteType.Bomb
                                              orderby note.JsonTime ascending,
                                                      note.Type == (int)NoteType.Bomb ? 0 : 1
                                              select note).ToList());
        }

        public void Run()
        {

            //leftSaber.RenderBombGroups();
            rightSaber.RenderBombGroups();
            // Might be better to do both hands at the same time for resets with both hands
            SaberSnapshot? firstL = this.leftSaber.FirstSwing();
            if (firstL != null)
            {
                redParities.Add(firstL.Value);
            }
            foreach ((ISimulationObject note1, ISimulationObject note2) in this.leftSaber.GetPairs())
            {
                SaberSnapshot? snapshot = this.leftSaber.Swing(note1, note2);
                if (snapshot != null)
                {
				    redParities.Add(snapshot.Value);
                    //Debug.Log(snapshot);
                }
            }

            redParities.AddRange(leftSaber.resetSnapshots);
            redParities = redParities.OrderByDescending(x => x.note.JsonTime).Reverse().ToList();

			SaberSnapshot? firstR = this.rightSaber.FirstSwing();
			if (firstR != null)
			{
				blueParities.Add(firstR.Value);
			}
			foreach ((ISimulationObject note1, ISimulationObject note2) in this.rightSaber.GetPairs())
			{
				SaberSnapshot? snapshot = this.rightSaber.Swing(note1, note2);
				if (snapshot != null)
				{
					blueParities.Add(snapshot.Value);
				}
			}

            blueParities.AddRange(rightSaber.resetSnapshots);
            blueParities = blueParities.OrderByDescending(x => x.note.JsonTime).Reverse().ToList();
        }

        private RightSaber rightSaber;
        private LeftSaber leftSaber;

        public List<SaberSnapshot> blueParities { get; private set; } = [];
        public List<SaberSnapshot> redParities { get; private set; } = [];

        
    }
    public record struct SaberSnapshot(ISimulationObject simObject, Vector3 position, Quaternion rotation, Parity parity, float wristAngle, bool reset = false)
    {

        public bool isBombGroup => simObject is BombGroup;
        public Vector3 hilt => position;
        public Vector3 tip => position + (Saber.length * (rotation * Vector3.up));

        public BaseNote note => simObject.GetNote();

        public float beat => simObject.Time();

        public override string ToString() => $"Beat: {beat}, CutDir: {note.CutDirection}, Position: {position}, Rotation: {rotation.eulerAngles}, Wrist Angle: {wristAngle}, Parity: {parity.ToString()}{(reset ? ", Reset" : "")}";
    }
}
