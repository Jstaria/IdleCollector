using IdleEngine;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

using System.Reflection;

namespace IdleCollector
{
    public class SpawnManager
    {
        private static SpawnManager instance;

        public static SpawnManager Instance { get
            {
                if (instance == null)
                    instance = new SpawnManager();

                return instance;
            } 
        }

        public SpawnManager()
        {
            Initialize();
        }

        private Dictionary<string, InteractableStats> floraStats;
        
        public InteractableStats GetStats(string name) => floraStats[name];

        public List<Type> GetSpawnedTypes()
        {
            List<Type> types = new List<Type>();

            for (int i = 0; i < floraStats.Count; i++)
            {
                List<string> keys = floraStats.Keys.ToList();

                double randomNum = RandomHelper.Instance.GetDouble();

                if (randomNum < floraStats[keys[i]].SpawnChance)
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    types.Add(assembly.GetType("IdleCollector." + keys[i]));
                }
            }

            return types;
        }

        private void Initialize()
        {
            CreateDict();
        }

        private void CreateDict()
        {
            floraStats = new();
            FileIO.ReadJsonInto(floraStats, "Content/SaveData/SpawnData.json");
        }

        private void UpdateDict()
        {
            floraStats = new();

            InteractableStats stats = new();
            stats.RareSpawnChance = 0.01f;
            stats.SpawnChance = 0.005f;
            stats.ProductionRate = .25f;
            stats.ClassName = "Cactus";

            floraStats.Add("Cactus", stats);

            FileIO.WriteJsonTo(floraStats, "Content/SaveData/SpawnData.json", Formatting.Indented);
        }
    }
}
