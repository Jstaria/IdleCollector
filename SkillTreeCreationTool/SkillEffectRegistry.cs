using System;
using System.Collections.Generic;

namespace SkillTreeCreationTool
{
    public sealed class SkillEffectDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public float Amount { get; set; }
        public string SkillEffectDescription { get; set; } = string.Empty;
    }

    public static class SkillEffectRegistry
    {
        private static readonly Dictionary<string, Action<SkillEffectDefinition>> handlers = new();

        public static void Register(string key, Action<SkillEffectDefinition> handler)
        {
            if (!handlers.TryAdd(key, handler))
                throw new InvalidOperationException($"Duplicate skill effect key: {key}");
        }

        public static  void Apply(SkillEffectDefinition effect)
        {
            if (!handlers.TryGetValue(effect.Key, out Action<SkillEffectDefinition> handler))
                throw new InvalidOperationException($"Unknown skill effect key: {effect.Key}");

            handler(effect);
        }
    }
}
