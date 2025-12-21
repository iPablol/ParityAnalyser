using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyser
{
    // Wrapper class for BaseNote to implement ISimulationObject
    public class Note(BaseNote note) : ISimulationObject
    {
        public BaseNote Value = note;

        public static implicit operator BaseNote(Note note) => note.Value;

        public float Time() => Value.JsonTime;

        public BaseNote FirstNote() => Value;
        public BaseNote LastNote() => Value;

        public IEnumerable<BaseNote> Notes() => [Value];
    }
}
