using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleCollector
{
    internal interface ISaveable
    {
        protected void Load();
        public void Save();
        public void Reset();
    }
}
