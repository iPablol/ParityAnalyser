
global using System.Numerics;
using NativeFileDialog.Extended;
using ParityAnalyserCore.Sim;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace ParityAnalyserProgram
{
	internal class Program
	{
		static void Main(string[] args)
		{
			string? path = args.FirstOrDefault();
			if (path is null)
			{
				path = NFD.OpenDialog("");
			}
			if (path is not null && File.Exists(path) && path.EndsWith(".dat"))
			{
				Simulation sim = new ([.. GetMapNotes(path)]);
				sim.Run();
				//foreach (var snap in sim.blueParities)
				//{
				//	Console.WriteLine(snap);
				//}
			}
			else
			{
				Console.WriteLine("Invalid path");
			}
		}

		private static IEnumerable<BaseNote> GetMapNotes(string path)
		{
			string json = File.ReadAllText(path);
			using JsonDocument doc = JsonDocument.Parse(json);
			foreach (JsonProperty jsonProperty in doc.RootElement.EnumerateObject())
			{
				string key = jsonProperty.Name;
				JsonElement node = jsonProperty.Value;
				// V3
				if (key == "colorNotes")
				{
					foreach (JsonElement el in node.EnumerateArray())
					{
						yield return new BaseNote(jsonTime: el.GetProperty("b").GetSingle(), 
													type: el.GetProperty("c").GetInt32(),
													cutDirection: el.GetProperty("d").GetInt32(),
													posX: el.GetProperty("x").GetInt32(), posY: el.GetProperty("y").GetInt32());
					}
				}
				else if (key == "bombNotes")
				{
					foreach (JsonElement el in node.EnumerateArray())
					{
						yield return new BaseNote(jsonTime: el.GetProperty("b").GetSingle(),
													type: (int)NoteType.Bomb,
													cutDirection: (int)NoteCutDirection.None,
													posX: el.GetProperty("x").GetInt32(), posY: el.GetProperty("y").GetInt32());
					}
				}
				// V2
				else if (key == "_notes")
				{
					foreach (JsonElement el in node.EnumerateArray())
					{
						yield return new BaseNote(jsonTime: el.GetProperty("_time").GetSingle(),
													type: el.GetProperty("_type").GetInt32(),
													cutDirection: el.GetProperty("_cutDirection").GetInt32(),
													posX: el.GetProperty("_lineIndex").GetInt32(), posY: el.GetProperty("_lineLayer").GetInt32());
					}
				}
			}
		}
	}
}
