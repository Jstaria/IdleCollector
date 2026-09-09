using System;
using System.Collections.Generic;

namespace IdleCollector
{
    public enum OptionsState { FadingIn, FadingOut }

    internal static class MenuData
    {
        public static int divisions = 20;
    }

    internal sealed class OptionsMenuConfig
    {
        public List<MenuDefinition> Menus { get; set; } = new();
    }

    internal sealed class MenuDefinition
    {
        public string Id { get; set; }
        public List<MenuItemDefinition> Items { get; set; } = new();
    }

    internal sealed class MenuItemDefinition
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public string Type { get; set; }
        public float Row { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string Binding { get; set; }
        public int? Steps { get; set; }
    }

    internal sealed class SliderBinding
    {
        public Func<float> GetValue { get; }
        public Action<float> SetValue { get; }

        public SliderBinding(Func<float> getValue, Action<float> setValue)
        {
            GetValue = getValue;
            SetValue = setValue;
        }
    }
}
