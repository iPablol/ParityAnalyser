using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace ParityAnalyser
{
    [Serializable]
    internal class Options
    {
        public bool renderLeftParitySabers = true;
        public bool renderRightParitySabers = true;
        
        public bool renderLeftParityOutlines = true;
        public bool renderRightParityOutlines = true;
        
        public bool animateLeftParities = true;
        public bool animateRightParities = true;

        public bool renderLeftBombGroups = false;
        public bool renderRightBombGroups = false;

        public bool debugLeftBombCollisions = false;
        public bool debugRightBombCollisions = false;

        public bool logResets = true;
        public bool renderInlinesAndInverts = false;

        [NonSerialized]private static readonly string savePath = Path.Combine(Application.persistentDataPath, "ParityAnalyser.json");

        public void Save()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(savePath, json);
        }

        public static Options Load()
        {
            if (!File.Exists(savePath))
                return new();

            try
            {
                var json = File.ReadAllText(savePath);
                return JsonConvert.DeserializeObject<Options>(json) ?? new();
            }
            catch
            {
                return new();
            }
        }
    }
}
