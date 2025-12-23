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
        public static readonly float bombRadius = 0.48f;
        public static readonly float sliderThreshold = 1 / 11.5f; // Thanks yabje for those 1/12 sliders


        public static readonly float minSaberX = -0.2f;
        public static readonly float maxSaberX = 3.2f;

        public static readonly float minSaberY = -0.2f;
        public static readonly float maxSaberY = 2.2f;

        public Simulation(List<BaseNote> notes)
        {
            // TODO: Check starting parity
            if (notes.Any(note => note.Type == (int)NoteType.Red))
            {
                this.leftSaber = new LeftSaber((from note in notes
                                               where note.Type == (int)NoteType.Red || note.Type == (int)NoteType.Bomb
                                               orderby note.JsonTime ascending,
                                                       note.Type == (int)NoteType.Bomb ? 0 : 1
                                               select note).ToList());
            }
            if (notes.Any(note => note.Type == (int)NoteType.Blue))
            {
                this.rightSaber = new RightSaber((from note in notes
                                              where note.Type == (int)NoteType.Blue || note.Type == (int)NoteType.Bomb
                                              orderby note.JsonTime ascending,
                                                      note.Type == (int)NoteType.Bomb ? 0 : 1
                                              select note).ToList());
            }
        }

        public void Run()
        {
            Debug.Log("-------------- Left Saber --------------");
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
            Debug.Log("----------------------------------------\n");
            Debug.Log("-------------- Right Saber --------------");
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
            Debug.Log("-----------------------------------------\n");
        }

        private RightSaber rightSaber = null;
        private LeftSaber leftSaber = null;

        public List<SaberSnapshot> blueParities { get; private set; } = [];
        public List<SaberSnapshot> redParities { get; private set; } = [];

        
    }

    // The rotation can be inferred from wristAngle and parity?
    public record struct SaberSnapshot(BaseNote note, Vector3 position, Quaternion rotation, Parity parity, float wristAngle, bool reset = false)
    {

        public Vector3 hilt => position;
        public Vector3 tip => position + (Saber.length * (rotation * Vector3.up));


        public float beat => note.JsonTime;

        public override string ToString() => $"Beat: {beat}, CutDir: {note.CutDirection}, Position: {position}, Rotation: {rotation.eulerAngles}, Wrist Angle: {wristAngle}, Parity: {parity.ToString()}{(reset ? ", Reset" : "")}";
    }
}
