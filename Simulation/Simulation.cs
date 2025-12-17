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
            foreach (BaseNote note in this.leftSaber)
            {
                redParities.Add(this.leftSaber.Swing(note));
            }

            foreach (BaseNote note in this.rightSaber)
            {
                blueParities.Add(this.rightSaber.Swing(note));
            }
        }

        private RightSaber rightSaber;
        private LeftSaber leftSaber;

        public List<SaberSnapshot> blueParities { get; private set; } = [];
        public List<SaberSnapshot> redParities { get; private set; } = [];

        
    }
    public record struct SaberSnapshot(BaseNote note, Transform transform, Parity parity, bool reset = false)
    {
        public Vector3 hilt => transform.position;
        public Vector3 tip => transform.position + (Saber.length * transform.up);
    }
}
