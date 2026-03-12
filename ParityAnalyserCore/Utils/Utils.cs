
using ParityAnalyserCore.Sim;

using System.Text.RegularExpressions;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using System;

namespace ParityAnalyserCore
{

    public interface IDebugRenderer
    {
        public abstract void RenderLine(Vector3 pos1, Vector3 pos2, Vector3 cStart, Vector3 cEnd, float width = 0.05f, BaseNote sync = null);
        public abstract void RenderSphere(Vector3 pos, float radius, Vector3 c, BaseNote sync = null);

        public abstract void RenderRect(OrientedRect r, Vector3 c, float width = 0.05f, BaseNote sync = null);

        public abstract void AddOutline(BaseNote note, Vector3 color);
	}

    internal static class DebugRenderer
    {
        internal static IDebugRenderer? renderer;

        internal static void RenderLine(Vector3 pos1, Vector3 pos2, Vector3 cStart, Vector3 cEnd, float width = 0.05f, BaseNote sync = null) => renderer?.RenderLine(pos1, pos2, cStart, cEnd, width, sync);
        internal static void RenderSphere(Vector3 pos, float radius, Vector3 c, BaseNote sync = null) => renderer?.RenderSphere(pos, radius, c, sync);
        internal static void RenderRect(OrientedRect r, Vector3 c, float width = 0.05f, BaseNote sync = null) => renderer?.RenderRect(r, c, width, sync);
        internal static void AddOutline(BaseNote note, Vector3 color) => renderer.AddOutline(note, color);
	}

    public class OverlappingPairIterator<T> : IEnumerable<(T, T)>
    {
        private readonly List<T> _list;
        private SingleItemBehaviour behaviour;
        public bool includeSingle
        {
            get; private set;
        }

        public OverlappingPairIterator(IEnumerable<T> list, bool includeSingle = false, SingleItemBehaviour behaviour = SingleItemBehaviour.PAIR_WITH_FIRST)
        {
            _list = list.ToList();
            this.includeSingle = includeSingle;
            this.behaviour = behaviour;
        }

        public OverlappingPairIterator(List<T> list, bool includeSingle = false)
        {
            _list = list;
            this.includeSingle = includeSingle;
        }

        public IEnumerator<(T, T)> GetEnumerator()
        {
            for (int i = 1; i < _list.Count; i++)
            {
                yield return (_list[i - 1], _list[i]);
            }
            if (includeSingle)
            {
                yield return (_list.Last(), behaviour switch
                {
					SingleItemBehaviour.PAIR_WITH_FIRST => _list.First(),
                    SingleItemBehaviour.PAIR_WITH_LAST => _list.Last(),
                    SingleItemBehaviour.PAIR_WITH_DEFAULT => default
                });
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public enum SingleItemBehaviour
        {
            PAIR_WITH_FIRST,
            PAIR_WITH_LAST,
            PAIR_WITH_DEFAULT,
        }
    }

	//public class SimulationIterator : IEnumerable<(object, object)> 
	//{
	//	private readonly List<object> _list;
	//	public bool includeSingle
	//	{
	//		get; private set;
	//	}

	//	public SimulationIterator(List<object> list, bool includeSingle = false)
	//	{
	//		_list = list;
	//		this.includeSingle = includeSingle;
	//	}

	//	public IEnumerator<(object, object)> GetEnumerator()
	//	{
	//		for (int i = 1; i < _list.Count; i++)
	//		{
 //               object next = _list[i];
 //               if (next is BaseNote note && note.Type == (int)NoteType.Bomb)
 //               {
 //                   next = GroupBombs(i);
 //                   i += (next as IEnumerable<object>).Count() - 1;
 //               }
	//			yield return (_list[i - 1], next);
	//		}
	//		if (includeSingle)
	//		{
	//			yield return (_list.Last(), _list.First());
	//		}
	//	}

 //       public IEnumerable<object> GroupBombs(int startIndex)
 //       {
 //           int i = startIndex;
 //           while (_list[i] is BaseNote note && note.Type == (int)NoteType.Bomb)
 //           {
 //               yield return _list[i++];
 //           }
 //       }

	//	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
	//}

    // Apparently you can't define extension properties with the same name for two different types in the same static class
    internal static class Vec2Extensions
    {
		extension(Vector2 a)
		{
			public float Cross(Vector2 b) => (a.X * b.Y) - (a.Y * b.X);
			public float SignedDistanceToPlane(Vector2 planePoint, Vector2 normalDirection)
			{

				Vector2 n = normalDirection.normalized;

				return Vector2.Dot(a - planePoint, n);
			}

			public Vector2 normalized => a / a.Length();
			public void Normalize()
			{
				Vector2 normalized = a.normalized;
				a.X = normalized.X; a.Y = normalized.Y;
			}



			public static Vector2 up => new(0, 1f);
			public static Vector2 down => new(0, -1f);
			public static Vector2 right => new(1f, 0);
			public static Vector2 left => new(-1f, 0);

			public static Vector2 upLeft => Vector2.up + Vector2.left;
			public static Vector2 upRight => Vector2.up + Vector2.right;
			public static Vector2 downLeft => Vector2.down + Vector2.left;
			public static Vector2 downRight => Vector2.down + Vector2.right;

			public static float SignedAngle(Vector2 from, Vector2 to)
			{
				float unsignedAngle = Angle(from, to);

				float cross = from.X * to.Y - from.Y * to.X;
				float sign = Math.Sign(cross);

				return unsignedAngle * sign;
			}
			public static float SignedAngleRad(Vector2 from, Vector2 to)
			{
				float unsignedAngle = AngleRad(from, to);

				float cross = from.X * to.Y - from.Y * to.X;
				float sign = Math.Sign(cross);

				return unsignedAngle * sign;
			}
			public static float Angle(Vector2 from, Vector2 to)
			{
                return AngleRad(from, to) * Math.Rad2Deg;
			}

            public static float AngleRad(Vector2 from, Vector2 to)
            {
				float denominator = (float)Math.Sqrt(from.LengthSquared() * to.LengthSquared());
				if (denominator < 1e-15f)
					return 0f;

				float dot = Math.Clamp(Vector2.Dot(from, to) / denominator, -1f, 1f);
                return (float)Math.Acos(dot);
			}

            public Vector3 ToVector3() => new(a.X, a.Y, 0);
		}
	}
	internal static class Utils
    {
		extension(Math)
		{
			public static float Deg2Rad => (float)(Math.PI / 180f);
            public static float Rad2Deg => 1 / Math.Deg2Rad;
			public static float DeltaAngle(float current, float target)
			{
				float delta = Repeat((target - current), 360f);
				if (delta > 180f)
					delta -= 360f;
				return delta;
			}

			public static float Repeat(float t, float length) => t - (float)Math.Floor(t / length) * length;

			public static float Lerp(float a, float b, float t)
			{
				t = Math.Clamp(t, 0f, 1f);
				return a + (b - a) * t;
			}

            public static float Clamp(float value, float min, float max) => Math.Min(Math.Max(min, value), max);

		}
		public static Vector2 gridCenter = new Vector2(1.5f, 1f);

		public static Vector3 RandomColor()
		{
            Random r = new();
			return new (
				(float)r.NextDouble(),  
				(float)r.NextDouble(),  
				(float)r.NextDouble()   
			);
		}
		public static float ClosestToZero(float a, float b) => Math.Abs(a) < Math.Abs(b) ? a : b;
        public static bool IsStackOrSlider(BaseNote note1, BaseNote note2) => Math.Abs(note2.JsonTime -  note1.JsonTime) <= Simulation.sliderThreshold;
        public static bool Bool(this Parity parity) => parity == Parity.FOREHAND;
        public static Parity ToParity(this bool b) => b ? Parity.FOREHAND : Parity.BACKHAND;
        public static Parity Other(this Parity parity) => (!parity.Bool()).ToParity();

        extension (Vector3 a)
        {
            public static Vector3 up => new (0, 1, 0);
            public static Vector3 right => new (1, 0, 0);
            public static Vector3 forward => new (0, 0, 1);

            public Vector2 ToVector2() => new (a.X, a.Y);
        }



        public static Vector2 DirectionFromDownAngle(float angleDeg)
        {
            float rad = (angleDeg - 90f) * Math.Deg2Rad;
            return new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad));
        }
        

        public static string CamelCaseToWords(this string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;

            // Insert space before capital letters that follow lowercase letters
            var result = Regex.Replace(
                input,
                @"(?<=[a-z])(?=[A-Z])",
                " "
            );

            // Capitalize first letter
            return char.ToUpper(result[0]) + result.Substring(1);
        }

        

        public static bool NearlyEqualTo(this float a, float b, float tolerance = 0.1f)
        {
            return Math.Abs(a - b) <= tolerance;
        }

    }
    
}
