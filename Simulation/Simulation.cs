using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Parity = ParityAnalyser.ParityAnalyser.Parity;

namespace ParityAnalyser.Sim
{
    public class Simulation
    {
        public static readonly float bombRadius = 0.9f;
        public Simulation(List<BaseNote> objects)
        {
            // TODO: Check starting parity
            this.leftSaber = new LeftSaber((from note in objects
                                           where note.Type == (int)NoteType.Red || note.Type == (int)NoteType.Bomb
                                           orderby note.JsonTime ascending
                                           select note).ToList());
            this.rightSaber = new RightSaber((from note in objects
                                              where note.Type == (int)NoteType.Blue || note.Type == (int)NoteType.Bomb
                                              orderby note.JsonTime ascending
                                              select note).ToList());
        }

        public void Run()
        {
            // Might be better to do both hands at the same time for resets with both hands
            SaberSnapshot? firstL = this.leftSaber.FirstSwing();
            if (firstL != null)
            {
                redParities.Add(firstL.Value);
            }
            foreach ((BaseNote note1, BaseNote note2) in this.leftSaber.GetPairs())
            {
                SaberSnapshot? snapshot = this.leftSaber.Swing(note1, note2);
                if (snapshot != null)
                {
				    redParities.Add(snapshot.Value);
                }
            }

			SaberSnapshot? firstR = this.rightSaber.FirstSwing();
			if (firstR != null)
			{
				blueParities.Add(firstR.Value);
			}
			foreach ((BaseNote note1, BaseNote note2) in this.rightSaber.GetPairs())
			{
				SaberSnapshot? snapshot = this.rightSaber.Swing(note1, note2);
				if (snapshot != null)
				{
					blueParities.Add(snapshot.Value);
                    Debug.Log(snapshot);
				}
			}
		}

        private RightSaber rightSaber;
        private LeftSaber leftSaber;

        public List<SaberSnapshot> blueParities { get; private set; } = [];
        public List<SaberSnapshot> redParities { get; private set; } = [];

        
    }
    public record struct SaberSnapshot(BaseNote note, Vector3 position, Quaternion rotation, Parity parity, float wristAngle, bool reset = false)
    {
        public Vector3 hilt => position;
        public Vector3 tip => position + (Saber.length * (rotation * Vector3.up));

        public override string ToString() => $"Beat: {note.JsonTime}, CutDir: {note.CutDirection}, Position: {position}, Rotation: {rotation.eulerAngles}, Wrist Angle: {wristAngle}, Parity: {parity.ToString()}{(reset ? ", Reset" : "")}";
    }
}
