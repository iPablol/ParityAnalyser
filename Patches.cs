using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ParityAnalyserCore.ParityAnalyser;

namespace ParityAnalyser
{
	[HarmonyPatch]
	internal class Patches
	{
		[HarmonyPatch(typeof(AutoSaveController), nameof(AutoSaveController.Save))]
		private static void Postfix(AutoSaveController __instance, bool auto = false)
		{
			if (!auto && Plugin.options.autoAnalyseOnSave)
			{
				Plugin.instance.Analyse();
			}
		}
	}
}
