
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyserCore.Sim
{
    public interface ISimulationObject
    {
        public float Time();

        public BaseNote FirstNote();

        public BaseNote LastNote();

        public IEnumerable<BaseNote> Notes();
    }
}
