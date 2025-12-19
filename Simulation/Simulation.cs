using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;
using Parity = ParityAnalyser.Parity;

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

            foreach (SaberSnapshot s in this.leftSaber.FirstSwing())
            {
                redParities.Add(s);
            }
            foreach ((ISimulationObject note1, ISimulationObject note2) in this.leftSaber.GetPairs())
            {
                foreach (SaberSnapshot snap in this.leftSaber.Swing(note1, note2))
                {
                    redParities.Add(snap);
                }
            }

            //redParities.AddRange(leftSaber.resetSnapshots);
            //redParities = redParities.OrderByDescending(x => x.note.JsonTime).Reverse().ToList();

            foreach (SaberSnapshot s in this.rightSaber.FirstSwing())
            {
                blueParities.Add(s);
            }
            foreach ((ISimulationObject note1, ISimulationObject note2) in this.rightSaber.GetPairs())
            {
                foreach (SaberSnapshot snap in this.rightSaber.Swing(note1, note2))
                {
                    blueParities.Add(snap);
                }
            }

            //foreach (SaberSnapshot snap in blueParities)
            //{
            //    Debug.Log(snap);
            //}

            //blueParities.AddRange(rightSaber.resetSnapshots);
            //blueParities = blueParities.OrderByDescending(x => x.note.JsonTime).Reverse().ToList();
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


        public float beat => note.JsonTime;

        public override string ToString() => $"Beat: {beat}, CutDir: {note.CutDirection}, Position: {position}, Rotation: {rotation.eulerAngles}, Wrist Angle: {wristAngle}, Parity: {parity.ToString()}{(reset ? ", Reset" : "")}";
    }
}
