using Beatmap.Base;
using Beatmap.Enums;

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
        public static ParityAnalyserCore.Sim.BaseNote ToInternal(this BaseNote note) => new(note.JsonTime, note.Type, note.CutDirection, note.PosX, note.PosY);

		public static Vector2 gridCenter = new Vector2(1.5f, 1f);
        public static Color RandomColor()
        {
            return new Color(
                Random.value,  // Red [0,1]
                Random.value,  // Green [0,1]
                Random.value   // Blue [0,1]
            );
        }

        public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 vec) => new(vec.X, vec.Y, vec.Z);

        public static UnityEngine.Quaternion ToUnity(this System.Numerics.Quaternion q) => new (q.X, q.Y, q.Z, q.W);

        public static Vector3 ToUnityVec3(this System.Numerics.Vector2 vec) => new(vec.X, vec.Y, 0);


        public static Vector3 Offset(this float time) => new Vector3(-1.5f, 0.5f, 0f);


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

    }
    
}
