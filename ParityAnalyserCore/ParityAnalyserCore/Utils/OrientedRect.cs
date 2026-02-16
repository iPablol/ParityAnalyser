using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace ParityAnalyser
{
    public struct OrientedRect
    {
        public Vector2 center;
        public Vector2 size;
        public float rotationDegrees;

        public OrientedRect(Vector2 center, Vector2 size, float rotationDegrees, float scale = 1f)
        {
            this.center = center;
            this.size = size * scale;
            this.rotationDegrees = rotationDegrees;
        }

        public OrientedRect(float left, float top, float width, float height, float scale = 1f)
        {
            this.center = new(left + width / 2, top + height / 2);
            this.size = new Vector2(width, height) * scale;
            this.rotationDegrees = 0f;
        }

        public float halfWidth => this.size.X / 2;
        public float halfHeight => this.size.Y / 2;

        public readonly Vector2 right => new ((float)Math.Cos(rotationDegrees * Math.Deg2Rad), (float)Math.Sin(rotationDegrees * Math.Deg2Rad));

        public readonly Vector2 up => new (-right.Y, right.X);

        public float xMin =>
            center.X - Math.Abs(right.X) * halfWidth - Math.Abs(up.X) * halfHeight;

        public float xMax =>
            center.X + Math.Abs(right.X) * halfWidth + Math.Abs(up.X) * halfHeight;

        public float yMin =>
            center.Y - Math.Abs(right.Y) * halfWidth - Math.Abs(up.Y) * halfHeight;

        public float yMax =>
            center.Y + Math.Abs(right.Y) * halfWidth + Math.Abs(up.Y) * halfHeight;

        public Vector2 tl => center - right * halfWidth + up * halfHeight;

        public Vector2 tr => center + right * halfWidth + up * halfHeight;

        public Vector2 bl => center - right * halfWidth - up * halfHeight;

        public Vector2 br => center + right * halfWidth - up * halfHeight;

        public bool Contains(Vector2 point)
        {
            Vector2 d = point - center;

            float dx = Vector2.Dot(d, right);
            float dy = Vector2.Dot(d, up);

            return Math.Abs(dx) <= halfWidth &&
                   Math.Abs(dy) <= halfHeight;
        }

        public static bool operator ==(OrientedRect a, OrientedRect b)
        {
            return a.center == b.center &&
                   a.size == b.size &&
                   a.rotationDegrees.NearlyEqualTo(b.rotationDegrees);
        }

        public static bool operator !=(OrientedRect a, OrientedRect b)
        {
            return !(a == b);
        }

    }
}
