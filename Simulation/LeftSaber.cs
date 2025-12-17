using Beatmap.Base;
using ParityAnalyser.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Parity = ParityAnalyser.ParityAnalyser.Parity;

namespace ParityAnalyser.Sim
{
    public class LeftSaber : Saber
    {
        public LeftSaber(List<BaseNote> relevantNotes, Parity start = Parity.BACKHAND) : base(relevantNotes, start)
        {
            this.transform.position = new Vector3(1f, 1f);
        }

        protected override float maxClockwiseAngle => -135f;

        protected override float maxCCAngle => 180f;

        public override void Reset(BaseNote culprit, Parity parity)
        {
            base.Reset(culprit, parity);
        }

        public override SaberSnapshot Swing(BaseNote nextNote)
        {

            return base.Swing(nextNote);
        }

        protected override void RotateTowards(BaseNote note)
        {
            base.RotateTowards(note);
        }

        protected override float DesiredAngle(NoteDirection dir) => this.parity switch
        {
            Parity.FOREHAND => dir switch
            {
                NoteDirection.UP => 180f,
                NoteDirection.DOWN => 0f,
                NoteDirection.LEFT => -90f,
                NoteDirection.RIGHT => 90f,
                NoteDirection.UP_LEFT => -135f,
                NoteDirection.UP_RIGHT => 135f,
                NoteDirection.DOWN_RIGHT => 45f,
                NoteDirection.DOWN_LEFT => -45f,
                NoteDirection.ANY => this.wristAngle,
                _ => 0f
            },
            Parity.BACKHAND => dir switch
            {
                NoteDirection.UP => 0f,
                NoteDirection.DOWN => 180f,
                NoteDirection.LEFT => 90f,
                NoteDirection.RIGHT => -90f,
                NoteDirection.UP_LEFT => 45f,
                NoteDirection.UP_RIGHT => -45f,
                NoteDirection.DOWN_RIGHT => -135f,
                NoteDirection.DOWN_LEFT => 135f,
                NoteDirection.ANY => this.wristAngle,
                _ => 0f
            },
            _ => 0
        };
    }
}
