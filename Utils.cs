using Beatmap.Base;
using ParityAnalyser.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;
using Parity = ParityAnalyser.ParityAnalyser.Parity;

namespace ParityAnalyser
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
    internal static class Utils
    {

        public static bool Bool(this Parity parity) => parity == Parity.FOREHAND;
        public static Parity ToParity(this bool b) => b ? Parity.FOREHAND : Parity.BACKHAND;
        public static Parity Other(this Parity parity) => (!parity.Bool()).ToParity();

        public static int CutDirFromVector(Vector2 direction)
        {
            direction = direction.normalized;
            Vector2 vector = (from v in DirectionalVectors
                              orderby Vector2.Dot(direction, v)
                              select v).First<Vector2>();
            Vector2 key = new Vector2((float)Math.Round((double)vector.x), (float)Math.Round((double)vector.y));
            return DirectionalVectorToCutDirection[key];
        }


        public static float Cross(this Vector2 a, Vector2 b) => (a.x * b.y) - (a.y * b.x);

        public static int CutDirFromNoteToNote(BaseNote firstNote, BaseNote lastNote)
        {
            return CutDirFromVector(new Vector2((float)lastNote.PosX, (float)lastNote.PosY) - new Vector2((float)firstNote.PosX, (float)firstNote.PosY));
        }

        //public static float cutAngle(this BaseNote note, float prevAngle = float.MaxValue) => (NoteDirection)note.CutDirection switch
        //{
        //    NoteDirection.DOWN => 0,
        //    NoteDirection.DOWN_RIGHT => 45,
        //    NoteDirection.RIGHT => 90,
        //    NoteDirection.UP_RIGHT => 135,
        //    NoteDirection.UP => 180,
        //    NoteDirection.UP_LEFT => -135,
        //    NoteDirection.LEFT => -90,
        //    NoteDirection.DOWN_LEFT => -45,
        //    NoteDirection.ANY => prevAngle + 180 > 180 ? prevAngle - 180 : prevAngle + 180, //opposing angle
        //    _ => 0
        //};

        //public static float toAngle(this NoteDirection dir) => dir switch
        //{
        //    NoteDirection.DOWN => 0,
        //    NoteDirection.DOWN_RIGHT => 45,
        //    NoteDirection.RIGHT => 90,
        //    NoteDirection.UP_RIGHT => 135,
        //    NoteDirection.UP => 180,
        //    NoteDirection.UP_LEFT => -135,
        //    NoteDirection.LEFT => -90,
        //    NoteDirection.DOWN_LEFT => -45,
        //    _ => 0
        //};

        public static Vector3 Offset(this BaseNote note) => new Vector3(-1.5f, -1.1f, (note.SongBpmTime - Shader.GetGlobalFloat("_SongTime")) * EditorScaleController.EditorScale);

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


        public static Vector2 Direction(this NoteDirection dir) => dir switch
        {
            NoteDirection.UP => Vector2.up,
            NoteDirection.DOWN => Vector2.down,
            NoteDirection.LEFT => Vector2.left,
            NoteDirection.RIGHT => Vector2.right,
            NoteDirection.UP_LEFT => Vector2.up + Vector2.left,
            NoteDirection.UP_RIGHT => Vector2.up + Vector2.right,
            NoteDirection.DOWN_LEFT => Vector2.down + Vector2.left,
            NoteDirection.DOWN_RIGHT => Vector2.down + Vector2.right,

        };

        public static readonly Vector2[] DirectionalVectors = new Vector2[]
        {
            new Vector2(0f, 1f),
            new Vector2(0f, -1f),
            new Vector2(-1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f)
        };

        public static readonly Dictionary<Vector2, int> DirectionalVectorToCutDirection = new Dictionary<Vector2, int>
        {
            {
                new Vector2(0f, 1f),
                0
            },
            {
                new Vector2(0f, -1f),
                1
            },
            {
                new Vector2(-1f, 0f),
                2
            },
            {
                new Vector2(1f, 0f),
                3
            },
            {
                new Vector2(-1f, 1f),
                4
            },
            {
                new Vector2(1f, 1f),
                5
            },
            {
                new Vector2(-1f, -1f),
                6
            },
            {
                new Vector2(1f, -1f),
                7
            },
            {
                new Vector2(0f, 0f),
                8
            }
        };

        public static readonly Dictionary<NoteDirection, NoteDirection> OpposingCutDict = new Dictionary<NoteDirection, NoteDirection>
        {
            {
                NoteDirection.UP,
                NoteDirection.DOWN
            },
            {
                NoteDirection.DOWN,
                NoteDirection.UP
            },
            {
                NoteDirection.LEFT,
                NoteDirection.RIGHT
            },
            {
                NoteDirection.RIGHT,
                NoteDirection.LEFT
            },
            {
                NoteDirection.UP_LEFT,
                NoteDirection.DOWN_RIGHT
            },
            {
                NoteDirection.DOWN_RIGHT,
                NoteDirection.UP_LEFT
            },
            {
                NoteDirection.UP_RIGHT,
                NoteDirection.DOWN_LEFT
            },
            {
                NoteDirection.DOWN_LEFT,
                NoteDirection.UP_RIGHT
            },
            {
                NoteDirection.ANY,
                NoteDirection.ANY
            }
        };
    }
}
