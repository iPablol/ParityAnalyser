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

        private IEnumerable<OrientedRect> GetAxisHitbox()
        {
            List<BaseNote> bombs = notes.ConvertAll(note => note.Value);
            var filled = new HashSet<Vector2Int>();

            for (int row = 0; row < 3; row++)
            {
                int? startCol = null;
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
                                1,
                                scale: 1.5f * Simulation.bombRadius
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
                        1,
                        scale: 1.5f * Simulation.bombRadius
                    );
                }
            }

        }

        private IEnumerable<OrientedRect> AddDiagonalConnectors(List<OrientedRect> rects, float cellSize = 1f)
        {
            for (int i = 0; i < rects.Count; i++)
            {
                for (int j = i + 1; j < rects.Count; j++)
                {
                    OrientedRect a = rects[i];
                    OrientedRect b = rects[j];

                    if (!AreDiagonallyTouching(a, b))
                        continue;

                    Vector2 dir = (b.center - a.center).normalized;

                    Vector2 center = (a.center + b.center) * 0.5f;

                    float length = cellSize * Mathf.Sqrt(2f);

                    float rotation =
                        Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                    yield return new OrientedRect(
                        center,
                        new Vector2(length, cellSize),
                        rotation,
                        scale: 1.5f * Simulation.bombRadius
                    );
                }
                yield return rects[i];
            }
        }
        private bool AreDiagonallyTouching(OrientedRect a, OrientedRect b)
        {
            Vector2 delta = a.center - b.center;
            return Mathf.Abs(delta.x).NearlyEqualTo(1f) && Mathf.Abs(delta.y).NearlyEqualTo(1f);
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
