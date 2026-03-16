
using System.Drawing;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System;
namespace ParityAnalyserCore.Sim
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

        public Vector2 NegativeSpaceCenter()
        {
            // Maybe bias towards saber's side (e.g.: right side for right saber)
			Vector2 sum = Vector2.Zero;
			float weightSum = 0f;

			for (int x = 0; x < 4; x++)
			{
				for (int y = 0; y < 3; y++)
				{
					if (!notes.Any(bomb => bomb.Value.PosX == x && bomb.Value.PosY == y))
					{
                        int adjacentBombs = notes.Count(bomb =>
                            Math.Abs(bomb.Value.PosX - x) == 1 ||
                            Math.Abs(bomb.Value.PosY - y) == 1);
						float weight = 1f / (float)Math.Pow(1f + adjacentBombs, 3.5f);

						Vector2 pos = new (x, y);

						sum += pos * weight;
						weightSum += weight;
					}
				}
			}

			Vector2 center = sum / weightSum;
			return center;
		}

        public IEnumerable<OrientedRect> GetHitbox()
        {
            foreach (OrientedRect rect in AddDiagonalConnectors(GetAxisHitbox().ToList())) yield return rect;
        }

        private IEnumerable<OrientedRect> GetAxisHitbox()
        {
            List<BaseNote> bombs = notes.ConvertAll(note => note.Value);
            var filled = new HashSet<Vector2>();

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
                                scale: 1.75f * Simulation.bombRadius
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
                        scale: 1.75f * Simulation.bombRadius
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

                    float length = cellSize * (float)Math.Sqrt(2f);

                    float rotation =
                        (float)Math.Atan2(dir.Y, dir.X) * Math.Rad2Deg;

                    yield return new OrientedRect(
                        center,
                        new Vector2(length, cellSize),
                        rotation,
                        scale: 1.75f * Simulation.bombRadius
                    );
                }
                yield return rects[i];
            }
        }
        private bool AreDiagonallyTouching(OrientedRect a, OrientedRect b)
        {
            Vector2 delta = a.center - b.center;
            return Math.Abs(delta.X).NearlyEqualTo(1f) && Math.Abs(delta.Y).NearlyEqualTo(1f);
        }

		public void Render()
		{
			foreach (OrientedRect r in GetHitbox())
			{
				DebugRenderer.RenderRect(r, new Vector3(0, 1, 0), sync: aBomb);
			}
		}
	}
}
