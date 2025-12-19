using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyser
{
    public record struct SliderGroup(BaseNote previousNote, List<BaseNote> slider) : ISimulationObject
    {
        public bool isFirstSwing => previousNote == null;

        public float Time() => slider.First().JsonTime;
        public BaseNote GetNote() => slider.First();
        public BaseNote LastNote() => slider.Last();

        public IEnumerable<BaseNote> Notes() => slider;
    }
}
