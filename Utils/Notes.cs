using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Beatmap.V4.V4CommonData;

namespace ParityAnalyser
{
    public static class Notes
    {
		public static UnityEngine.Vector3 Offset(this BaseNote note) => new (-1.5f, 0.5f, note.zPos());
		public static float zPos(this BaseNote note) => (note.SongBpmTime - Shader.GetGlobalFloat("_SongTime")) * EditorScaleController.EditorScale;
        public static BaseNote? FromInternal(this ParityAnalyserCore.Sim.BaseNote note, NoteGridContainer grid)
        {
            //Debug.Log(note.JsonTime);
            //Debug.Log("");
            //foreach (var n in grid.MapObjects.Where(o => (int)o.Type == (int)note.type))
            //{
            //    Debug.Log($"{n.JsonTime}");
            //}
            try
            {
                return grid.MapObjects.Where(o => o.JsonTime == note.JsonTime && (int)o.Type == (int)note.type && o.PosX == note.PosX && o.PosY == note.PosY).First();
            }
            catch
            {
                return null;
            }
		}

	}
}
