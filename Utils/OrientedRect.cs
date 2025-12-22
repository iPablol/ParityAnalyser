using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ParityAnalyser
{
    public struct OrientedRect
    {
        public Vector2 center;
        public Vector2 size;
        public float rotationDegrees;

        public OrientedRect(Vector2 center, Vector2 size, float rotationDegrees)
        {
            this.center = center;
            this.size = size;
            this.rotationDegrees = rotationDegrees;
        }

        public OrientedRect(float left, float top, float width, float height)
        {
            this.center = new(left + width / 2, top + height / 2);
            this.size = new(width, height);
            this.rotationDegrees = 0f;
        }

        public float halfWidth => this.size.x / 2;
        public float halfHeight => this.size.y / 2;

        public Vector2 right => new(Mathf.Cos(rotationDegrees * Mathf.Deg2Rad), Mathf.Sin(rotationDegrees * Mathf.Deg2Rad));

        public Vector2 up => new(-right.y, right.x);

        public float xMin =>
            center.x - Mathf.Abs(right.x) * halfWidth - Mathf.Abs(up.x) * halfHeight;

        public float xMax =>
            center.x + Mathf.Abs(right.x) * halfWidth + Mathf.Abs(up.x) * halfHeight;

        public float yMin =>
            center.y - Mathf.Abs(right.y) * halfWidth - Mathf.Abs(up.y) * halfHeight;

        public float yMax =>
            center.y + Mathf.Abs(right.y) * halfWidth + Mathf.Abs(up.y) * halfHeight;

        public Vector2 tl => center - right * halfWidth + up * halfHeight;

        public Vector2 tr => center + right * halfWidth + up * halfHeight;

        public Vector2 bl => center - right * halfWidth - up * halfHeight;

        public Vector2 br => center + right * halfWidth - up * halfHeight;

        public bool Contains(Vector2 point)
        {
            Vector2 d = point - center;

            float dx = Vector2.Dot(d, right);
            float dy = Vector2.Dot(d, up);

            return Mathf.Abs(dx) <= halfWidth &&
                   Mathf.Abs(dy) <= halfHeight;
        }

        public static bool operator ==(OrientedRect a, OrientedRect b)
        {
            return a.center == b.center &&
                   a.size == b.size &&
                   a.rotationDegrees.NearlyEqual(b.rotationDegrees);
        }

        public static bool operator !=(OrientedRect a, OrientedRect b)
        {
            return !(a == b);
        }

    }
}
