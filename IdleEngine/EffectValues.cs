using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public struct EffectValues
    {
        public KeyValuePair<string, float>[] floats;
        public KeyValuePair<string, int>[] ints;
        public KeyValuePair<string, bool>[] bools;
        public KeyValuePair<string, Matrix>[] matrices;
    }
}
