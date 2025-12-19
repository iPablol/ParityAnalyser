using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyser
{
    public interface ISimulationObject
    {
        public float Time();

        public BaseNote GetNote();

        public BaseNote LastNote();

        public IEnumerable<BaseNote> Notes();
    }
}
