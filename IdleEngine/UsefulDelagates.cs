using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public delegate Vector2 GetVector();
    public delegate float GetFloat();
    public delegate T Curve<T>(float t);
    public delegate float Curve(float t);
}
