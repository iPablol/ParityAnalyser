using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParityAnalyser
{
    public static class Notes
    {
        public static int CutDirFromNoteToNote(BaseNote firstNote, BaseNote lastNote)
        {
            return CutDirFromVector(new Vector2((float)lastNote.PosX, (float)lastNote.PosY) - new Vector2((float)firstNote.PosX, (float)firstNote.PosY));
        }

        public static Vector2 Position(this BaseNote note) => new Vector2(note.PosX, note.PosY);

        public static bool BottomRow(this BaseNote note) => note.PosY == 0;
        public static bool MiddleRow(this BaseNote note) => note.PosY == 1;
        public static bool TopRow(this BaseNote note) => note.PosY == 2;

        public static bool LeftOuterLane(this BaseNote note) => note.PosX == 0;
        public static bool LeftInnerLane(this BaseNote note) => note.PosX == 1;
        public static bool RightInnerLane(this BaseNote note) => note.PosX == 2;
        public static bool RightOuterLane(this BaseNote note) => note.PosX == 3;

        public static int CutDirFromVector(Vector2 direction)
        {
            direction = direction.normalized;
            Vector2 vector = (from v in DirectionalVectors
                              orderby Vector2.Dot(direction, v)
                              select v).First<Vector2>();
            Vector2 key = new Vector2((float)Math.Round((double)vector.x), (float)Math.Round((double)vector.y));
            return DirectionalVectorToCutDirection[key];
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

        public static bool isInner(this BaseNote note) => note.PosX > 0 && note.PosX < 3;
        public static bool isMiddle(this BaseNote note) => note.isInner() && note.PosY == 2;

    }
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
