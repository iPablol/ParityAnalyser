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

        public static bool SegmentIntersectsRect(Vector2 p1, Vector2 p2, OrientedRect rect)
        {
            if (rect.Contains(p1) || rect.Contains(p2))
                return true;

            return SegmentIntersectsSegment(p1, p2, rect.tl, rect.tr) ||
                SegmentIntersectsSegment(p1, p2, rect.tr, rect.br) ||
                SegmentIntersectsSegment(p1, p2, rect.br, rect.bl) ||
                SegmentIntersectsSegment(p1, p2, rect.bl, rect.tl);

            return false;
        }

        // Check if two segments (p1->p2 and q1->q2) intersect
        private static bool SegmentIntersectsSegment(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            // Using cross products
            float d = (p2.x - p1.x) * (q2.y - q1.y) - (p2.y - p1.y) * (q2.x - q1.x);
            if (Mathf.Approximately(d, 0)) return false; // Parallel

            float u = ((q1.x - p1.x) * (q2.y - q1.y) - (q1.y - p1.y) * (q2.x - q1.x)) / d;
            float v = ((q1.x - p1.x) * (p2.y - p1.y) - (q1.y - p1.y) * (p2.x - p1.x)) / d;

            return u >= 0f && u <= 1f && v >= 0f && v <= 1f;
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

        public static bool CircleRectIntersection(Vector2 circleCenter, float radius, OrientedRect rect, out Vector2 closestPoint)
        {
            // Clamp circle center to rectangle bounds → closest point on rectangle
            float x = Mathf.Clamp(circleCenter.x, rect.xMin, rect.xMax);
            float y = Mathf.Clamp(circleCenter.y, rect.yMin, rect.yMax);
            closestPoint = new Vector2(x, y);

            // Distance from circle center to closest point
            float distanceSq = (closestPoint - circleCenter).sqrMagnitude;

            return distanceSq <= radius * radius;
        }

        // With small angles it could be approximated with a triangle
        // https://math.stackexchange.com/questions/177857/circular-sector-to-circle-intersection#:~:text=To%20determine%20if%20a%20circular%20sector%20intersects,radius%20r%20distance%20from%20the%20center%20point
        public static bool SwingPathIntersects(Vector2 position, float startAngle, float endAngle, Vector2 bombPosition, bool debug = false, BaseNote bombForDebug = null)
        {
			Vector2 boundaryA = position + (Utils.DirectionFromDownAngle(startAngle) * Saber.length); // Boundary point B
			Vector2 boundaryB = position + (Utils.DirectionFromDownAngle(endAngle) * Saber.length); // Boundary point B
            if (debug)
            {
                Utils.RenderLine((Vector3)position, (Vector3)boundaryA, Color.blue, Color.blue, 0.02f, sync: bombForDebug);
                Utils.RenderLine((Vector3)position, (Vector3)boundaryB, Color.red, Color.red, sync: bombForDebug);

                //Utils.RenderLine((Vector3)bombPosition, (Vector3)bombPosition + zVec + Vector3.up * Simulation.bombRadius, Color.black, Color.black, 0.3f, bombForDebug);
                //Utils.RenderSphere((Vector3)bombPosition, Simulation.bombRadius, Color.cyan, bombForDebug);
                //Debug.Log("");
                //Debug.Log(startAngle); Debug.Log(endAngle);
                //Debug.Log("");
            }
            // Circles are too far apart
            if (Vector2.Distance(position, bombPosition) > Saber.length + Simulation.bombRadius) return false;


            // Sector boundary intersects
            bool boundaryAIntersects = SegmentIntersectsCircle(position, boundaryA, bombPosition, Simulation.bombRadius), boundaryBIntersects = SegmentIntersectsCircle(position, boundaryB, bombPosition, Simulation.bombRadius);
            if (boundaryAIntersects || boundaryBIntersects)
            {
                if (debug)
                {
                    Debug.Log("Boundary intersection");
                    //Debug.Log($"Position: {position}");
                    //Debug.Log($"Boundary A: {boundaryA} ({boundaryAIntersects}), Boundary B: {boundaryB} ({boundaryBIntersects})");
                    //Debug.Log($"Bomb position: {bombPosition}");
                    //Debug.Log("");
                }
                return true;
            }
            // Bomb intersects the arc
            else if (CircleCircleIntersection(position, Saber.length, bombPosition, Simulation.bombRadius, out Vector2 p1, out Vector2 p2))
            {
                float alpha = Vector2.Angle(boundaryA - position, boundaryB - position);
                float beta = Vector2.Angle(boundaryA - position, p1 - position);
                float gamma = Vector2.Angle(boundaryB - position, p1 - position);


                if (beta < alpha && gamma < alpha)
                {
                    if (debug)
                    {
                        Debug.Log($"P1 Alpha: {alpha}, beta: {beta}, gamma: {gamma}");
                        //Debug.Log($"A: {boundaryA}, B: {boundaryB}, P1: {p1}");
                        Utils.RenderLine((Vector3)position, (Vector3)p1, Color.green, Color.green, sync: bombForDebug);
                        Utils.RenderLine(position, p2, Color.green, Color.green, sync: bombForDebug);
                        Debug.Log("P1");
                    }
                    return true;
                }

                beta = Vector2.Angle(boundaryA - position, p2 - position);
                gamma = Vector2.Angle(boundaryB - position, p2 - position);
                if (beta < alpha && gamma < alpha)
                {
                    if (debug)
                    {
                        Debug.Log($"P2 Alpha: {alpha}, beta: {beta}, gamma: {gamma}");
                        //Debug.Log($"A: {boundaryA}, B: {boundaryB}, P2{p2}");
                        Utils.RenderLine((Vector3)position, (Vector3)p1, Color.green, Color.green, sync: bombForDebug);
                        Utils.RenderLine((Vector3)position, (Vector3)p2, Color.green, Color.green, sync: bombForDebug);
                        Debug.Log("P2");
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

        public static bool SwingPathIntersects(Vector2 position, float startAngle, float endAngle, OrientedRect hitbox, bool debug = false, BaseNote bombForDebug = null)
        {
            Vector2 boundaryA = position + (Utils.DirectionFromDownAngle(startAngle) * Saber.length); // Boundary point B
            Vector2 boundaryB = position + (Utils.DirectionFromDownAngle(endAngle) * Saber.length); // Boundary point B
            if (debug)
            {
                Utils.RenderLine((Vector3)position, (Vector3)boundaryA, Color.blue, Color.blue, 0.02f, sync: bombForDebug);
                Utils.RenderLine((Vector3)position, (Vector3)boundaryB, Color.red, Color.red, sync: bombForDebug);

                //Utils.RenderRect(hitbox, Color.green, sync: bombForDebug);
            }


            // Sector boundary intersects (includes inside rect case)
            bool boundaryAIntersects = SegmentIntersectsRect(position, boundaryA, hitbox), boundaryBIntersects = SegmentIntersectsRect(position, boundaryB, hitbox);
            if (boundaryAIntersects || boundaryBIntersects)
            {
                if (debug)
                {
                    Debug.Log("Boundary intersection");
                }
                return true;
            }

            // Bomb intersects the arc
            else if (CircleRectIntersection(position, Saber.length, hitbox, out Vector2 point))
            {
                // TODO: check if this is fixed (example: seraph's trial beat 268)
                Vector2 dir = (point - position).normalized;
                Vector2 a = (boundaryA - position).normalized;
                Vector2 b = (boundaryB - position).normalized;

                float dirAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float alpha = Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg;
                float beta = Mathf.Atan2(b.y, b.x) * Mathf.Rad2Deg;

                // Wrap all angles to [0, 360)
                dirAngle = Mathf.Repeat(dirAngle, 360f);
                alpha = Mathf.Repeat(alpha, 360f);
                beta = Mathf.Repeat(beta, 360f);

                float delta = Mathf.Repeat(beta - alpha, 360f);  // CCW from alpha → beta

                // If delta > 180, take the complementary (smaller) arc
                if (delta > 180f)
                {
                    float temp = alpha;
                    alpha = beta;
                    beta = temp;
                    delta = 360f - delta;
                }

                float test = Mathf.Repeat(dirAngle - alpha, 360f);
                if (test <= delta)
                {
                    if (debug)
                    {
                        Utils.RenderLine((Vector3)position, (Vector3)point, Color.green, Color.green, sync: bombForDebug);
                        Debug.Log($"alpha: {alpha}, beta: {beta}, dirAngle: {dirAngle}, delta: {delta}");
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
