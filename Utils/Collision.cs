using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParityAnalyser
{
	internal static class Collision
	{
		public static bool SegmentIntersectsCircle(Vector2 a, Vector2 b, Vector2 center, float radius)
		{
			Vector2 d = b - a;
			Vector2 f = a - center;

			float A = Vector2.Dot(d, d);
			float B = 2 * Vector2.Dot(f, d);
			float C = Vector2.Dot(f, f) - (radius * radius);

			float discriminant = (B * B) - (4 * (A * C));
			if (discriminant < 0)
				return false;

			discriminant = Mathf.Sqrt(discriminant);

			float t1 = (-B - discriminant) / (2 * A);
			float t2 = (-B + discriminant) / (2 * A);

			return (t1 >= 0 && t1 <= 1) || (t2 >= 0 && t2 <= 1);
		}
	}
}
