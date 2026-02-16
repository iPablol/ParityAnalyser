

using System.Numerics;
using ParityAnalyser.Sim;

namespace ParityAnalyser
{
    public static class Notes
    {

        //public static Vector3 Offset(this BaseNote note) => new Vector3(-1.5f, 0.5f, note.zPos());
        //public static float zPos(this BaseNote note) => (note.SongBpmTime - Shader.GetGlobalFloat("_SongTime")) * EditorScaleController.EditorScale;

        public static Vector2 Position(this BaseNote note) => new Vector2(note.PosX, note.PosY);
        //public static Vector3 Position3(this BaseNote note) => new Vector3(note.PosX, note.PosY, note.zPos());

        public static bool BottomRow(this BaseNote note) => note.PosY == 0;
        public static bool MiddleRow(this BaseNote note) => note.PosY == 1;
        public static bool TopRow(this BaseNote note) => note.PosY == 2;

        public static bool LeftOuterLane(this BaseNote note) => note.PosX == 0;
        public static bool LeftInnerLane(this BaseNote note) => note.PosX == 1;
        public static bool RightInnerLane(this BaseNote note) => note.PosX == 2;
        public static bool RightOuterLane(this BaseNote note) => note.PosX == 3;

        public static Vector2 BombDodgeCenter(this BaseNote bomb)
        {
            Vector2 offset = default;
            switch (bomb.PosY)
            {
                case 0:
                    offset = bomb.PosX > 1 ? 
                        // Bottom right
                        new(0.5f, -0.5f) :
                        // Bottom left
                        new(-0.5f, -0.5f);
                    break;
                case 1:
                    if (bomb.RightOuterLane())
                    {
                        // Middle right
                        offset = new(0.5f, 0f);
                    }
                    else if (bomb.LeftOuterLane())
                    {
                        // Middle left
                        offset = new(-0.5f, 0f);
                    }
                    break;
                case 2:
                    offset = bomb.PosX > 1 ?
                        // Top right
                        new(0.5f, 0.5f) :
                        // Top left
                        new(-0.5f, 0.5f);
                    break;

                default: break;
            }

            return bomb.Position() + offset;
        }

    }
}
