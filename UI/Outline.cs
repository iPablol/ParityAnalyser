using Beatmap.Base;
using Beatmap.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;

namespace ParityAnalyser
{
	partial class SpawnedNote
	{
		public float Beat { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }
		public int Color { get; set; }
		public Color outlineColor { get; set; }
	}
	internal class Outline
	{
		private ParityAnalyser plugin;

		public HashSet<SpawnedNote> outlineCache = new HashSet<SpawnedNote>();

		public Outline(ParityAnalyser plugin)
		{
			this.plugin = plugin;
			plugin.noteGrid.ContainerSpawnedEvent += SetOutline;
		}
		public void AddToCache(BaseNote note, Color outlineColor)
		{
			outlineCache.Add(new SpawnedNote()
			{
				Beat = note.JsonTime, PosX = note.PosX, PosY = note.PosY, Color = note.Color, outlineColor = outlineColor
			});
		}

		public void SetOutline(BaseObject baseObject)
		{
			if (baseObject is BaseNote note)
			{
				SpawnedNote spawned = new SpawnedNote()
				{
					Beat = note.JsonTime,
					PosX = note.PosX,
					PosY = note.PosY,     
					Color = note.Color,
				};
				SetOutlineColor(spawned);
			}
		}

		public void SetOutlineColor(SpawnedNote mapObject)
		{
			try
			{
				var collection = BeatmapObjectContainerCollection.GetCollectionForType(ObjectType.Note);
				SpawnedNote n = outlineCache.Where(x => x.Beat <= mapObject.Beat + 0.01 && x.Beat >= mapObject.Beat - 0.01 && x.PosX == mapObject.PosX && x.PosY == mapObject.PosY && x.Color == mapObject.Color).First();
				if (n != null)
				{
					var container = collection.LoadedContainers.Where((item) =>
					{
						if (item.Key is BaseNote note)
						{
							if (note.JsonTime <= mapObject.Beat + 0.01 && note.JsonTime >= mapObject.Beat - 0.01 && note.PosX == mapObject.PosX && note.PosY == mapObject.PosY && note.Color == mapObject.Color)
							{
								return true;
							}
						}
						return false;
					}).First().Value;
					container.SetOutlineColor(n.outlineColor);
				}
			}
			catch (InvalidOperationException ex)
			{
				if (ex.Message != "Sequence contains no elements")
				{
					throw;
				}
				// dont need to do anything, objects just not inside the loaded range.
			}

		}

		internal void RefreshOutlines()
		{
			plugin.noteGrid.RefreshPool(true);
		}
	}
}

