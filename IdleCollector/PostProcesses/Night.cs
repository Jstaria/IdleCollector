using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace IdleEngine.PostProcesses
{
    public class Night : PostProcess
    {
        public const int MaxLights = 64;

        public sealed class Light
        {
            public Vector2 Position { get; set; }
            public Color Tint { get; set; }
            public float Brightness { get; set; }
            public float Radius { get; set; }
            public float Softness { get; set; }
            public float Falloff { get; set; }

            public Light(Vector2 position, Color tint, float brightness, float radius, float softness, float falloff = .25f)
            {
                Position = position;
                Tint = tint;
                Brightness = brightness;
                Radius = radius;
                Softness = softness;
                Falloff = falloff;
            }
        }

        private static Night instance;

        public static Night Instance => instance ??= new Night();

        private readonly Effect effect;
        private RenderTarget2D outputTexture;
        private readonly List<Light> lights = new();
        private readonly Vector4[] lightGeometryData = new Vector4[MaxLights];
        private readonly Vector4[] lightColorData = new Vector4[MaxLights];
        private readonly Vector4[] lightFalloffData = new Vector4[MaxLights];
        private Texture2D lightGeometryTexture;
        private Texture2D lightColorTexture;
        private Texture2D lightFalloffTexture;

        public float Darkness { get; set; } = .7f;
        public float LightRadius { get; set; } = 1f;
        public float LightSoftness { get; set; } = 100f;
        public Color NightTint { get; set; } = new Color(120, 120, 180);

        public Night()
        {
            instance ??= this;
            effect = ResourceAtlas.GetEffect("NightOverlay");
        }

        public override void Draw(
            SpriteBatch sb,
            ref RenderTarget2D normalTexture,
            RenderTarget2D uiTexture,
            ref RenderTarget2D combinedTexture)
        {
            if (sb == null)
                throw new ArgumentNullException(nameof(sb));
            if (normalTexture == null)
                throw new ArgumentNullException(nameof(normalTexture));

            EnsureOutputTexture(sb.GraphicsDevice, normalTexture);

            int lightCount = PopulateShaderLights(normalTexture);
            EnsureLightDataTextures(sb.GraphicsDevice);
            lightGeometryTexture.SetData(lightGeometryData);
            lightColorTexture.SetData(lightColorData);
            lightFalloffTexture.SetData(lightFalloffData);
            effect.Parameters["screenSize"]?.SetValue(normalTexture.Bounds.Size.ToVector2());
            effect.Parameters["darkness"]?.SetValue(MathHelper.Clamp(Darkness, 0f, 1f));
            effect.Parameters["nightTint"]?.SetValue(NightTint.ToVector3());
            effect.Parameters["lightGeometryTexture"]?.SetValue(lightGeometryTexture);
            effect.Parameters["lightColorTexture"]?.SetValue(lightColorTexture);
            effect.Parameters["lightFalloffTexture"]?.SetValue(lightFalloffTexture);
            effect.Parameters["lightCount"]?.SetValue(lightCount);
            effect.CurrentTechnique = effect.Techniques["NightOverlay"];

            sb.GraphicsDevice.SetRenderTarget(outputTexture);
            sb.GraphicsDevice.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, effect);
            sb.Draw(normalTexture, outputTexture.Bounds, Color.White);
            sb.End();

            normalTexture = outputTexture;
            sb.GraphicsDevice.SetRenderTarget(null);
        }

        public Light AddLight(Vector2 position, Color tint, float brightness, float radius, float softness, float falloff = .25f)
        {
            return AddLight(new Light(position, tint, brightness, radius, softness, falloff));
        }

        public Light TryAddLight(Vector2 position, Color tint, float brightness, float radius, float softness, float falloff = .25f)
        {
            if (lights.Count >= MaxLights)
                return null;

            return AddLight(position, tint, brightness, radius, softness, falloff);
        }

        public Light AddLight(Light light)
        {
            if (light == null)
                throw new ArgumentNullException(nameof(light));
            if (lights.Count >= MaxLights)
                throw new InvalidOperationException($"Night supports at most {MaxLights} lights.");

            lights.Add(light);
            return light;
        }

        public bool RemoveLight(int index)
        {
            if (index < 0 || index >= lights.Count)
                return false;

            lights.RemoveAt(index);
            return true;
        }

        public bool RemoveLight(Light light)
        {
            return light != null && lights.Remove(light);
        }

        public void ClearLights()
        {
            lights.Clear();
        }

        public void SetLight(Vector2 position, Color tint, float brightness)
        {
            ClearLights();
            AddLight(position, tint, brightness, LightRadius, LightSoftness);
        }

        private int PopulateShaderLights(RenderTarget2D target)
        {
            Array.Clear(lightGeometryData, 0, lightGeometryData.Length);
            Array.Clear(lightColorData, 0, lightColorData.Length);
            Array.Clear(lightFalloffData, 0, lightFalloffData.Length);

            int count = 0;
            for (int i = 0; i < lights.Count && count < MaxLights; i++)
                AddShaderLight(lights[i], target, ref count);

            return count;
        }

        private void AddShaderLight(Light light, RenderTarget2D target, ref int count)
        {
            Vector2 shaderPosition = light.Position;
            shaderPosition.X *= target.Width / (float)Renderer.RenderSize.X;
            shaderPosition.Y *= target.Height / (float)Renderer.RenderSize.Y;

            lightGeometryData[count] = new Vector4(
                shaderPosition.X / target.Width,
                shaderPosition.Y / target.Height,
                Math.Max(0f, light.Radius) / target.Height,
                Math.Max(0.0001f, light.Softness) / target.Height);
            lightColorData[count] = new Vector4(light.Tint.ToVector3(), Math.Max(0f, light.Brightness));
            lightFalloffData[count] = new Vector4(MathHelper.Clamp(light.Falloff, .05f, 1f), 0, 0, 0);
            count++;
        }

        private void EnsureLightDataTextures(GraphicsDevice graphicsDevice)
        {
            if (lightGeometryTexture != null && lightColorTexture != null && lightFalloffTexture != null)
                return;

            lightGeometryTexture?.Dispose();
            lightColorTexture?.Dispose();
            lightFalloffTexture?.Dispose();
            lightGeometryTexture = new Texture2D(graphicsDevice, MaxLights, 1, false, SurfaceFormat.Vector4);
            lightColorTexture = new Texture2D(graphicsDevice, MaxLights, 1, false, SurfaceFormat.Vector4);
            lightFalloffTexture = new Texture2D(graphicsDevice, MaxLights, 1, false, SurfaceFormat.Vector4);
        }

        private void EnsureOutputTexture(GraphicsDevice graphicsDevice, RenderTarget2D source)
        {
            if (outputTexture != null && outputTexture.Width == source.Width &&
                outputTexture.Height == source.Height && outputTexture.Format == source.Format)
                return;

            outputTexture?.Dispose();
            outputTexture = new RenderTarget2D(
                graphicsDevice,
                source.Width,
                source.Height,
                false,
                source.Format,
                DepthFormat.None);
        }
    }
}
