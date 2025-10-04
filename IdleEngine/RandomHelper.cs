using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

namespace IdleEngine
{
    public class RandomHelper
    {
        private static RandomHelper instance;

        private Random random = new Random();
        private int seed;

        public static RandomHelper Instance
        {
            get
            {
                if (instance == null)
                    instance = new RandomHelper();

                return instance;
            }
        }

        public RandomHelper()
        {
            random = new Random();
            seed = random.Next(0, int.MaxValue);
            random = new Random(seed);
        }

        public void SetSeed(int seed) => random = new Random(seed);
        public int GetSeed() => seed;

        public int GetInt(int min, int max) => random.Next(min, max + 1);
        public int GetIntExclusive(int min, int max) => random.Next(min, max);
        public int[] GetInts(int numberOf, int min, int max)
        {
            int[] ints = new int[numberOf];

            ints = Enumerable
                .Range(0,numberOf)
                .Select(w => random.Next(min,max + 1))
                .ToArray();

            return ints;
        }

        public double GetDouble(float scale = 1) => random.NextDouble() * scale;
        public double[] GetDoubles(int numberOf, float scale = 1)
        {
            double[] doubles = new double[numberOf];

            doubles = Enumerable
                .Range(0, numberOf)
                .Select(w => GetDouble(scale))
                .ToArray();

            return doubles;
        }

        public float GetFloat(float min, float max)
        {
            float t = (float)GetDouble();

            return MathHelper.Lerp(min, max, t);
        }
        public float[] GetFloats(int numberOf, float min, float max)
        {
            float[] floats = new float[numberOf];

            floats = Enumerable
                .Range(0, numberOf)
                .Select(w => GetFloat(min, max))
                .ToArray();

            return floats;
        }

        public Vector2 GetVector2(Rectangle bounds) => GetVector2(bounds.Location.ToVector2(), (bounds.Location + bounds.Size).ToVector2());
        public Vector2 GetVector2(Vector2 pos1, Vector2 pos2)
        {
            Vector2 size = pos2 - pos1;

            Vector2 position = new Vector2(
                GetFloat(pos1.X, pos1.X + size.X),
                GetFloat(pos1.Y, pos1.Y + size.Y));

            return position;
        }
        public Vector2[] GetVector2s(int numberOf, Rectangle bounds)
        {
            Vector2[] vector2s = new Vector2[numberOf];

            vector2s = Enumerable
                .Range(0, numberOf)
                .Select(w => GetVector2(bounds))
                .ToArray();

            return vector2s;
        }
        public Vector2[] GetVector2s(int numberOf, Vector2 pos1, Vector2 pos2)
        {
            Vector2[] vector2s = new Vector2[numberOf];

            vector2s = Enumerable
                .Range(0, numberOf)
                .Select(w => GetVector2(pos1, pos2))
                .ToArray();

            return vector2s;
        }

        public bool GetBool() => GetInt(0, 1) == 0;
        public bool[] GetBools(int numberOf)
        {
            bool[] bools = new bool[numberOf];

            bools = Enumerable
                .Range(0, numberOf)
                .Select(w => GetBool())
                .ToArray();

            return bools;
        }

        public Color GetColor(Color a, Color b)
        {
            float t = (float)GetDouble();

            return Color.Lerp(a, b, t);
        }
    }
}
