using Beatmap.Base;
using Beatmap.Enums;
using ParityAnalyser.Sim;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;
using Parity = ParityAnalyser.Parity;

using Random = UnityEngine.Random;

namespace ParityAnalyser
{
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
	internal static class Utils
    {
        public static Vector2 gridCenter = new Vector2(1.5f, 1f);
        public static Color RandomColor()
        {
            return new Color(
                Random.value,  // Red [0,1]
                Random.value,  // Green [0,1]
                Random.value   // Blue [0,1]
            );
        }

        public static float ClosestToZero(float a, float b) => Math.Abs(a) < Math.Abs(b) ? a : b;
        public static bool IsStackOrSlider(BaseNote note1, BaseNote note2) => Math.Abs(note2.JsonTime -  note1.JsonTime) <= Simulation.sliderThreshold;
        public static bool Bool(this Parity parity) => parity == Parity.FOREHAND;
        public static Parity ToParity(this bool b) => b ? Parity.FOREHAND : Parity.BACKHAND;
        public static Parity Other(this Parity parity) => (!parity.Bool()).ToParity();

        // TODO: clear all renders button
        public static void RenderLine(Vector3 pos1, Vector3 pos2, Color colorStart, Color colorEnd, float width = 0.05f, BaseNote sync = null)
        {
            GameObject renderer = new GameObject("line");
            LineRenderer lr = renderer.AddComponent<LineRenderer>();
            lr.positionCount = 2;

            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(colorStart, 0f),
                    new GradientColorKey(colorEnd, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 1f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            lr.colorGradient = g;
            lr.startWidth = lr.endWidth = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            var atc = ParityAnalyser.atc;
            lr.SetPositions([pos1 + sync.Offset(), pos2 + sync.Offset()]);
            Action update = () =>
            {
                float time = atc.CurrentJsonTime;
                lr.SetPositions([pos1 + sync.Offset(), pos2 + sync.Offset()]);

            };
            ParityAnalyser.AddRender(renderer, update);
        }

        public static void RenderSphere(Vector3 pos, float radius, Color color, BaseNote sync = null)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.transform.localScale = Vector3.one * radius * 2f;

            var renderer = sphere.GetComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = color;

            var atc = ParityAnalyser.atc;
            sphere.transform.position = pos + (sync?.Offset() ?? default);
            Action update = () =>
            {
                float time = atc.CurrentJsonTime;
                sphere.transform.position = pos + (sync?.Offset() ?? default);
            };
            ParityAnalyser.AddRender(sphere, update);

        }

        public static void RenderRect(Rect r, Color color, float width = 0.05f, BaseNote sync = null)
        {
            Vector2 tl = new(r.x, r.y), tr = tl + new Vector2(r.width, 0), bl = tl + new Vector2(0, r.height), br = tl + new Vector2(r.width, r.height);
            GameObject renderer = new GameObject("rect");
            LineRenderer lr = renderer.AddComponent<LineRenderer>();
            lr.positionCount = 5;

            Gradient g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 1f),
                    new GradientAlphaKey(1f, 1f)
                }
            );

            lr.colorGradient = g;
            lr.startWidth = lr.endWidth = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));

            var atc = ParityAnalyser.atc;
            lr.SetPositions([(Vector3)tl + sync.Offset(), (Vector3)tr + sync.Offset(), (Vector3)br + sync.Offset(), (Vector3)bl + sync.Offset(), (Vector3)tl + sync.Offset()]);
            Action update = () =>
            {
                float time = atc.CurrentJsonTime;
                lr.SetPositions([(Vector3)tl + sync.Offset(), (Vector3)tr + sync.Offset(), (Vector3)br + sync.Offset(), (Vector3)bl + sync.Offset(), (Vector3)tl + sync.Offset()]);

            };
            ParityAnalyser.AddRender(renderer, update);
        }

        public static float Cross(this Vector2 a, Vector2 b) => (a.x * b.y) - (a.y * b.x);

        

        public static Vector3 Offset(this float time) => new Vector3(-1.5f, 0.5f, 0f);


        public static Vector2 DirectionFromDownAngle(float angleDeg)
        {
            float rad = (angleDeg - 90f) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        public static float DownAngleBetween(Vector2 a, Vector2 b)
        {
            float a1 = AngleFromDown(a);
            float a2 = AngleFromDown(b);
            return Mathf.DeltaAngle(a1, a2);
        }

        public static float AngleFromDown(Vector2 v)
        {
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            angle = 90f - angle;
            if (angle < 0) angle += 360f;
            return angle;
        }

        public static float DownAngleFromDir(Vector2 dir)
        {
            dir.Normalize();

            // Standard atan2 gives angle with 0 at right
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // Rotate reference so 0 is down
            angle = 90f - angle;



            return angle;
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

        public static float SignedDistanceToPlane(this Vector2 p, Vector2 planePoint, Vector2 normalDirection)
        {
            
            Vector2 n = normalDirection.normalized;

            return Vector2.Dot(p - planePoint, n);
        }

        public static bool NearlyEqual(this float a, float b, float tolerance = 0.1f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

    }
    
}
