using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public interface IAnimatable: IRenderable
    {
        public Point FrameCount { get; protected set; }
        public Point CurrentFrame { get; protected set; }
        public bool IsPlaying { get; protected set; }
        public float FrameSpeed { get; protected set; }
        public void SetFrame(int x, int y);
        public void Play();
        public void Pause();
        public void NextFrame();
        public void PrevFrame();
    }
}
