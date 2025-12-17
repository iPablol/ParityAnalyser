using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using Parity = ParityAnalyser.ParityAnalyser.Parity;

namespace ParityAnalyser.Sim
{
    public class RightSaber : Saber
    {
        public RightSaber(List<BaseNote> relevantNotes, Parity start = Parity.BACKHAND) : base(relevantNotes, start)
        {
            this.transform.position = new Vector3(2f, 1f);
        }

        public override void Reset(BaseNote culprit, Parity parity)
        {
            base.Reset(culprit, parity);
        }

        public override SaberSnapshot Swing(BaseNote nextNote)
        {

            return base.Swing(nextNote);
        }

    }
}
