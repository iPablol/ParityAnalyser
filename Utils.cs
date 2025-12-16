using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace ParityAnalyser
{
    internal static class Utils
    {
        public class OverlappingPairIterator<T> : IEnumerable<(T, T)>
        {
            private readonly List<T> _list;
            public bool includeSingle
            {
                get; private set;
            }

            public OverlappingPairIterator(List<T> list, bool includeSingle = false)
            {
                _list = list;
                this.includeSingle = includeSingle;
            }

            public IEnumerator<(T, T)> GetEnumerator()
            {
                for (int i = 1; i < _list.Count; i++)
                {
                    yield return (_list[i - 1], _list[i]);
                }
                if (includeSingle)
                {
                    yield return (_list.Last(), _list.First());
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static float cutAngle(this BaseNote note) => (NoteDirection)note.CutDirection switch
        {
            NoteDirection.DOWN => 0,
            NoteDirection.DOWN_RIGHT => 45,
            NoteDirection.RIGHT => 90,
            NoteDirection.UP_RIGHT => 135,
            NoteDirection.UP => 180,
            NoteDirection.UP_LEFT => -135,
            NoteDirection.LEFT => -90,
            NoteDirection.DOWN_LEFT => -45,
            _ => 0
        };

        public enum NoteDirection
        {
            UP = 0,
            DOWN = 1,
            LEFT = 2,
            RIGHT = 3,
            UP_LEFT = 4,
            UP_RIGHT = 5,
            DOWN_LEFT = 6,
            DOWN_RIGHT = 7,
            ANY = 8
        }
    }
}
