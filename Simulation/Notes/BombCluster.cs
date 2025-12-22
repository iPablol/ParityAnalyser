using Beatmap.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;

namespace ParityAnalyser.Sim
{
    public record struct BombCluster(List<Note> notes, float time)
    {
        public BaseNote aBomb
        {
            get
            {
                float time = this.time;
                return (from bomb in notes where bomb.Time() == time select bomb).First();
            }
        }

        public IEnumerable<OrientedRect> GetHitbox()
        {
            foreach (OrientedRect rect in AddDiagonalConnectors(GetAxisHitbox().ToList())) yield return rect;
        }

        public IEnumerable<OrientedRect> GetAxisHitbox()
        {
            List<BaseNote> bombs = notes.ConvertAll(note => note.Value);
            var filled = new HashSet<Vector2Int>();

            for (int row = 0; row < 3; row++)
            {
                int? startCol = null; // moved outside the column loop
                for (int col = 0; col < 4; col++)
                {
                    bool hasBomb = bombs.Any(bomb => bomb.PosX == col && bomb.PosY == row);

                    if (!hasBomb)
                    {
                        // No bomb → end of run
                        if (startCol != null)
                        {
                            yield return new OrientedRect(
                                startCol.Value - 0.5f,
                                row - 0.5f,
                                col - startCol.Value,
                                1
                            );
                            startCol = null;
                        }
                    }
                    else
                    {
                        // Bomb found → start run if not already started
                        if (startCol == null)
                            startCol = col;
                    }
                }

                // Handle run that reaches the end of the row
                if (startCol != null)
                {
                    yield return new OrientedRect(
                        startCol.Value - 0.5f,
                        row - 0.5f,
                        4 - startCol.Value,
                        1
                    );
                }
            }

        }

        public static IEnumerable<OrientedRect> AddDiagonalConnectors(List<OrientedRect> rects, float cellSize = 1f)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                for (int j = i + 1; j < rects.Count; j++)
                {
                    OrientedRect a = rects[i];
                    OrientedRect b = rects[j];

                    if (!AreDiagonallyTouching(a, b))
                        continue;

                    if (HasOrthogonalNeighbor(rects, a, b))
                        continue;

                    // Direction from A to B (diagonal)
                    Vector2 dir = (b.center - a.center).normalized;

                    // Midpoint between cell centers
                    Vector2 center = (a.center + b.center) * 0.5f;

                    // Diagonal length across a cell
                    float length = cellSize * Mathf.Sqrt(2f);

                    // Rotation from direction
                    float rotation =
                        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    yield return new OrientedRect(
                        center,
                        new Vector2(length, cellSize),
                        rotation
                    );
                }
                yield return rects[i];
            }
        }
        private static bool AreDiagonallyTouching(OrientedRect a, OrientedRect b)
        {
            return
                (a.xMax == b.xMin && a.yMax == b.yMin) ||
                (a.xMin == b.xMax && a.yMax == b.yMin) ||
                (a.xMax == b.xMin && a.yMin == b.yMax) ||
                (a.xMin == b.xMax && a.yMin == b.yMax);
        }

        private static Vector2 GetTouchingCorner(OrientedRect from, OrientedRect to)
        {
            if (from.xMax == to.xMin && from.yMax == to.yMin)
                return new Vector2(from.xMax, from.yMax);

            if (from.xMin == to.xMax && from.yMax == to.yMin)
                return new Vector2(from.xMin, from.yMax);

            if (from.xMax == to.xMin && from.yMin == to.yMax)
                return new Vector2(from.xMax, from.yMin);

            return new Vector2(from.xMin, from.yMin);
        }

        private static bool HasOrthogonalNeighbor(List<OrientedRect> rects, OrientedRect a, OrientedRect b)
        {
            Vector2 mid = (GetTouchingCorner(a, b) + GetTouchingCorner(b, a)) * 0.5f;

            foreach (var r in rects)
            {
                if (r == a || r == b)
                    continue;

                if (r.Contains(mid))
                    return true;
            }

            return false;
        }

        public void Render()
        {
            foreach (OrientedRect r in GetHitbox())
            {
                Utils.RenderRect(r, Color.green, sync: aBomb);
            }
        }
    }
}
