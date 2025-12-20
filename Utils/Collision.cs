using Beatmap.Base;
using ParityAnalyser.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;

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

        public static bool CircleCircleIntersection(Vector2 c0, float r0, Vector2 c1, float r1, out Vector2 p0, out Vector2 p1)
        {
            p0 = p1 = Vector2.zero;

            float d = Vector2.Distance(c0, c1);

            // No solution cases
            if (d > r0 + r1) return false;           // separate
            if (d < Mathf.Abs(r0 - r1)) return false; // one inside another
            if (d == 0f && r0 == r1) return false;   // coincident

            // Distance from c0 to the midpoint between intersections
            float a = (r0 * r0 - r1 * r1 + d * d) / (2f * d);

            // Height from midpoint to intersection points
            float hSq = r0 * r0 - a * a;
            if (hSq < 0f) hSq = 0f; // numerical safety
            float h = Mathf.Sqrt(hSq);

            // Point along the center line
            Vector2 p = c0 + a * (c1 - c0) / d;

            // Perpendicular offset
            Vector2 offset = h * new Vector2(
                -(c1.y - c0.y) / d,
                 (c1.x - c0.x) / d
            );

            p0 = p + offset;
            p1 = p - offset;

            return true;
        }

        // https://math.stackexchange.com/questions/177857/circular-sector-to-circle-intersection#:~:text=To%20determine%20if%20a%20circular%20sector%20intersects,radius%20r%20distance%20from%20the%20center%20point
        public static bool SwingPathIntersects(Vector2 position, float startAngle, float endAngle, Vector2 bombPosition, bool debug = false, BaseNote bombForDebug = null)
        {
			Vector2 boundaryA = position + (Utils.DirectionFromDownAngle(startAngle) * Saber.length); // Boundary point B
			Vector2 boundaryB = position + (Utils.DirectionFromDownAngle(endAngle) * Saber.length); // Boundary point B
            if (debug)
            {
                Utils.RenderLine((Vector3)position, (Vector3)boundaryA, Color.blue, Color.blue, sync: bombForDebug);
                Utils.RenderLine((Vector3)position, (Vector3)boundaryB, Color.red, Color.red, sync: bombForDebug);

                //Utils.RenderLine((Vector3)bombPosition, (Vector3)bombPosition + zVec + Vector3.up * Simulation.bombRadius, Color.black, Color.black, 0.3f, bombForDebug);
                Utils.RenderSphere((Vector3)bombPosition, Simulation.bombRadius, Color.cyan, bombForDebug);
                //Debug.Log("");
                //Debug.Log(startAngle); Debug.Log(endAngle);
                //Debug.Log("");
            }
            // Circles are too far apart
            if (Vector2.Distance(position, bombPosition) > Saber.length + Simulation.bombRadius) return false;


            // Sector boundary intersects
            if (SegmentIntersectsCircle(position, boundaryA, bombPosition, Simulation.bombRadius) || SegmentIntersectsCircle(position, boundaryB, bombPosition, Simulation.bombRadius))
            {
                if (debug)
                {
                    Debug.Log("Boundary intersection");
                }
                return true;
            }
            // Bomb is inside the radius
            else if (Vector2.Distance(position, bombPosition) < Saber.length)
            {
                float alpha = Vector2.Angle(boundaryA, boundaryB);
                float beta = Vector2.Angle(boundaryA, bombPosition - position);
                if (beta < alpha)
                {
                    if (debug)
                    {
                        Debug.Log("Contained");
                    }
                    return true;
                }
            }
            // Bomb intersects the arc
            else if (CircleCircleIntersection(position, Saber.length, bombPosition, Simulation.bombRadius, out Vector2 p1, out Vector2 p2))
            {
                float alpha = Vector2.Angle(boundaryA - position, boundaryB - position);
                float beta = Vector2.Angle(boundaryA - position, p1 - position);


                if (beta < alpha)
                {
                    if (debug)
                    {
                        //Debug.Log($"P1 Alpha: {alpha}, beta: {beta}");
                        //Debug.Log($"A: {boundaryA}, B: {boundaryB}, P1: {p1}");
                        Utils.RenderLine((Vector3)position, (Vector3)p1, Color.green, Color.green, sync: bombForDebug);
                        Utils.RenderLine(position, p2, Color.green, Color.green, sync: bombForDebug);
                    }
                    return true;
                }

                beta = Vector2.Angle(boundaryA - position, p2 - position);
                if (beta < alpha)
                {
                    if (debug)
                    {
                        //Debug.Log($"P2 Alpha: {alpha}, beta: {beta}, P2{p2}");
                        //Debug.Log($"A: {boundaryA}, B: {boundaryB}");
                        Utils.RenderLine((Vector3)position, (Vector3)p1, Color.green, Color.green, sync: bombForDebug);
                        Utils.RenderLine((Vector3)position, (Vector3)p2, Color.green, Color.green, sync: bombForDebug);
                    }
                    return true;
                }

            }
            else
            {
                return false;
            }
            return false;
        }
	}

}
