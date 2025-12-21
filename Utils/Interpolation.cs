using ParityAnalyser.Sim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParityAnalyser
{
    internal class Interpolation
    {
        public static Vector3 SamplePositionAtTime(IReadOnlyList<SaberSnapshot> points, float t)
        {
            if (points == null || points.Count == 0)
                return Vector3.zero;

            // Clamp outside range
            if (t <= points[0].beat)
                return points[0].position;

            if (t >= points[points.Count - 1].beat)
                return points[points.Count - 1].position;

            // Binary search for efficiency
            int lo = 0;
            int hi = points.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;

                if (points[mid].beat < t)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            // hi < lo; hi is before t, lo is after t
            SaberSnapshot a = points[hi];
            SaberSnapshot b = points[lo];

            float u = Mathf.InverseLerp(a.beat, b.beat, t);
            return Sperp(a.position, b.position, u);
        }

        public static float SampleWristAngleAtTime(IReadOnlyList<SaberSnapshot> points, float t)
        {
            if (points == null || points.Count == 0)
                return 0;

            // Clamp outside range
            if (t <= points[0].beat)
                return points[0].wristAngle;

            if (t >= points[points.Count - 1].beat)
                return points[points.Count - 1].wristAngle;

            // Binary search for efficiency
            int lo = 0;
            int hi = points.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;

                if (points[mid].beat < t)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            // hi < lo; hi is before t, lo is after t
            SaberSnapshot a = points[hi];
            SaberSnapshot b = points[lo];

            float u = Mathf.InverseLerp(a.beat, b.beat, t);
            return Mathf.Lerp(a.wristAngle, b.wristAngle, u) - 90f;
        }

        public static Quaternion SampleRotationAtTime(IReadOnlyList<SaberSnapshot> points, float t)
        {
            if (points == null || points.Count == 0)
                return Quaternion.identity;

            // Clamp outside range
            if (t <= points[0].beat)
                return points[0].rotation;

            if (t >= points[points.Count - 1].beat)
                return points[points.Count - 1].rotation;

            // Binary search for efficiency
            int lo = 0;
            int hi = points.Count - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;

                if (points[mid].beat < t)
                    lo = mid + 1;
                else
                    hi = mid - 1;
            }

            // hi < lo; hi is before t, lo is after t
            SaberSnapshot a = points[hi];
            SaberSnapshot b = points[lo];

            float u = Mathf.InverseLerp(a.beat, b.beat, t);
            return AngleSpikeSlerp(a.rotation, b.rotation, u);
        }

        public static Vector3 Sperp(Vector3 a, Vector3 b, float t, float sharpness = 2.5f)
        {
            t = Mathf.Clamp01(t);

            float w;
            if (t < 0.5f)
                w = 0.5f * Mathf.Pow(t * 2f, sharpness);
            else
                w = 1f - (0.5f * Mathf.Pow((1f - t) * 2f, sharpness));

            return (a * (1f - w)) + (b * w);
        }

        public static Quaternion AngleSpikeSlerp(Quaternion a, Quaternion b, float t, float angleWidth = 180f)
        {
            float angle = Quaternion.Angle(a, b);
            float w = Mathf.Exp(-(angle * angle) / (angleWidth * angleWidth));
            return Quaternion.Slerp(a, b, w * t);
        }

        public static Quaternion AngleSpikeSlerpXZ( Quaternion a, Quaternion b, float t, float xAngleWidth = 180f, float zAngleWidth = 10f)
        {
            t = Mathf.Clamp01(t);

            // delta rotation
            Quaternion d = Quaternion.Inverse(a) * b;

            // extract signed angles
            float dx = ExtractAngleAroundAxis(d, Vector3.right);
            //float dz = ExtractAngleAroundAxis(d, Vector3.forward);

            // spike weights per axis
            float wx = Mathf.Exp(-(dx * dx) / (xAngleWidth * xAngleWidth));
            //float wz = Mathf.Exp(-(dz * dz) / (zAngleWidth * zAngleWidth));

            // apply scaled angles
            Quaternion qx = Quaternion.AngleAxis(dx * wx * t, Vector3.right);
            //Quaternion qz = Quaternion.AngleAxis(dz * wz * t, Vector3.forward);

            // recombine (order matters!)
            return a * qx;
        }

        static float ExtractAngleAroundAxis(Quaternion q, Vector3 axis)
        {
            q.ToAngleAxis(out float angle, out Vector3 qAxis);
            if (Vector3.Dot(qAxis, axis) < 0f)
                angle = -angle;
            return angle;
        }
    }
}
