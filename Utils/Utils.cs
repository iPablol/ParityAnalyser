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
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;
using Parity = ParityAnalyser.ParityAnalyser.Parity;

using Random = UnityEngine.Random;

namespace ParityAnalyser
{
    public class OverlappingPairIterator<T> : IEnumerable<(T, T)>
    {
        private readonly List<T> _list;
        public bool includeSingle
        {
            get; private set;
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
                yield return (_list.Last(), _list.First());
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
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

        public static Color RandomColor()
        {
            return new Color(
                Random.value,  // Red [0,1]
                Random.value,  // Green [0,1]
                Random.value   // Blue [0,1]
            );
        }

        public static float ClosestToZero(float a, float b) => Math.Abs(a) < Math.Abs(b) ? a : b;
        public static float sliderThreshold = 1 / 11.5f; // Thanks yabje for those 1/12 sliders
        public static bool IsStackOrSlider(BaseNote note1, BaseNote note2) => Math.Abs(note2.JsonTime -  note1.JsonTime) <= sliderThreshold;
        public static bool Bool(this Parity parity) => parity == Parity.FOREHAND;
        public static Parity ToParity(this bool b) => b ? Parity.FOREHAND : Parity.BACKHAND;
        public static Parity Other(this Parity parity) => (!parity.Bool()).ToParity();



        public static float Cross(this Vector2 a, Vector2 b) => (a.x * b.y) - (a.y * b.x);

        

        public static Vector3 Offset(this BaseNote note) => new Vector3(-1.5f, 0.5f, (note.SongBpmTime - Shader.GetGlobalFloat("_SongTime")) * EditorScaleController.EditorScale);
        public static Vector3 Offset(this float time) => new Vector3(-1.5f, 0.5f, 0f);

        


        
    }
    
}
