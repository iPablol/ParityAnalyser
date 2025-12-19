using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyser
{
    internal class Note(BaseNote note) : ISimulationObject
    {
        public BaseNote Value = note;

        public static implicit operator BaseNote(Note note) => note.Value;

        public float Time() => Value.JsonTime;

        public BaseNote GetNote() => Value;
        public BaseNote LastNote() => Value;

        public IEnumerable<BaseNote> Notes() => [Value];
    }
}
