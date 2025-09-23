using IdleEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    public abstract class Interactable
    {
        protected Interactable() { }

        public abstract void InteractWith(ICollidable collider);
    }
}
