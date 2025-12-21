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
    public record struct BombCluster(List<Note> notes)
    {
        public BaseNote aBomb => notes.First()?.Value;

        public IEnumerable<Rect> GetHitbox()
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
                            yield return new Rect(
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
                    yield return new Rect(
                        startCol.Value - 0.5f,
                        row - 0.5f,
                        4 - startCol.Value,
                        1
                    );
                }
            }

        }

        public void Render()
        {
            foreach (Rect r in GetHitbox())
            {
                Vector2 tl = new(r.x, r.y), tr = tl + new Vector2(r.width, 0), bl = tl + new Vector2(0, r.height), br = tl + new Vector2(r.width, r.height);
                Utils.RenderLine(tl, tr, Color.green, Color.green, sync: aBomb);
                Utils.RenderLine(tr, br, Color.green, Color.green, sync: aBomb);
                Utils.RenderLine(br, bl, Color.green, Color.green, sync: aBomb);
                Utils.RenderLine(bl, tl, Color.green, Color.green, sync: aBomb);
            }
        }
    }
}
