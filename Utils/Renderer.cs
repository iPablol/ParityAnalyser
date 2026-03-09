using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using ParityAnalyserCore;
using ParityAnalyserCore.Sim;

namespace ParityAnalyser
{
	internal class Renderer(Plugin plugin) : IDebugRenderer
	{
		void IDebugRenderer.RenderLine(System.Numerics.Vector3 pos1, System.Numerics.Vector3 pos2, System.Numerics.Vector3 cStart, System.Numerics.Vector3 cEnd, float width = 0.05f, BaseNote s = null)
		{
			Beatmap.Base.BaseNote? sync = s == null ? null : s.FromInternal(plugin.noteGrid);
			Color colorStart = new(cStart.X, cStart.Y, cStart.Z); Color colorEnd = new(cEnd.X, cEnd.Y, cEnd.Z);
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

			var atc = Plugin.atc;
			lr.SetPositions([pos1.ToUnity() + sync?.Offset() ?? Vector3.zero, pos2.ToUnity() + sync?.Offset() ?? Vector3.zero]);
			Action update = () =>
			{
				float time = atc.CurrentJsonTime;
				lr.SetPositions([pos1.ToUnity() + sync?.Offset() ?? Vector3.zero, pos2.ToUnity() + sync?.Offset() ?? Vector3.zero]);

			};
			Plugin.AddRender(renderer, update);
		}

		void IDebugRenderer.RenderSphere(System.Numerics.Vector3 pos, float radius, System.Numerics.Vector3 c, BaseNote s = null)
		{
			Beatmap.Base.BaseNote? sync = s == null ? null : s.FromInternal(plugin.noteGrid);
			Color color = new(c.X, c.Y, c.Z);
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

			sphere.transform.localScale = Vector3.one * radius * 2f;

			var renderer = sphere.GetComponent<MeshRenderer>();
			renderer.material = new Material(Shader.Find("Unlit/Color"));
			renderer.material.color = color;

			var atc = Plugin.atc;
			sphere.transform.position = pos.ToUnity() + (sync?.Offset() ?? default);
			Action update = () =>
			{
				float time = atc.CurrentJsonTime;
				sphere.transform.position = pos.ToUnity() + (sync?.Offset() ?? default);
			};
			Plugin.AddRender(sphere, update);

		}

		void IDebugRenderer.RenderRect(OrientedRect r, System.Numerics.Vector3 c, float width = 0.05f, BaseNote s = null)
		{
			Beatmap.Base.BaseNote? sync = s == null ? null : s.FromInternal(plugin.noteGrid);
			Color color = new(c.X, c.Y, c.Z);
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

			var atc = Plugin.atc;
			lr.SetPositions([r.tl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.tr.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.br.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.bl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.tl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero]);
			Action update = () =>
			{
				float time = atc.CurrentJsonTime;
				lr.SetPositions([r.tl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.tr.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.br.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.bl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero, r.tl.ToUnityVec3() + sync?.Offset() ?? Vector3.zero]);

			};
			Plugin.AddRender(renderer, update);
		}

		void IDebugRenderer.AddOutline(BaseNote note, System.Numerics.Vector3 color)
		{
			Beatmap.Base.BaseNote n = note.FromInternal(plugin.noteGrid);
			if (n is null) return;
			Plugin.outline.AddToCache(n, new Color(color.X, color.Y, color.Z));
		}
	}
}
