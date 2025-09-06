using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdleEngine
{
    public struct BatchConfig
    {
        public readonly SpriteSortMode sortMode;
        public readonly BlendState blendState;
        public readonly SamplerState samplerState;
        public readonly DepthStencilState depthStencilState;
        public readonly RasterizerState rasterizerState;
        public readonly Effect effect;
        public readonly EffectValues[] effectValues;
        public readonly Matrix transformMatrix;

        public BatchConfig(
            SpriteSortMode sortMode, 
            BlendState blendState, 
            SamplerState samplerState, 
            DepthStencilState depthStencilState, 
            RasterizerState rasterizerState, 
            Effect effect, 
            EffectValues[] effectValues,
            Matrix transformMatrix)
        {
            this.sortMode = sortMode;
            this.blendState = blendState;
            this.samplerState = samplerState;
            this.depthStencilState = depthStencilState;
            this.rasterizerState = rasterizerState;
            this.effect = effect;
            this.effectValues = effectValues;
            this.transformMatrix = transformMatrix;
        }
    }
}
