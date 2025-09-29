using Microsoft.VisualBasic.FileIO;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public class SpawnManager
    {
        private static SpawnManager instance;
        private Random rand;

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

                double randomNum = rand.NextDouble();

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

            rand = new Random();
        }

        private void CreateDict()
        {
            floraStats = new();

            List<string> data = FileIO.ReadFrom("SpawnData", "SaveData");

            foreach (string key in data)
            {
                string[] dataLine = key.Split("|");

                Object stats = new InteractableStats();

                Type type = typeof(InteractableStats);

                string[] statsData = dataLine[1].Split(",");

                foreach (string property in statsData)
                {
                    string[] splitLine = property.Split(":");

                    FieldInfo field = type.GetField(splitLine[0].Trim(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (field != null)
                    {
                        field.SetValue(stats, Convert.ChangeType(splitLine[1].Trim(), field.FieldType));
                    }
                }

                floraStats.Add(dataLine[0], (InteractableStats)stats);
            }
        }
    }
}
