using System;

namespace SkillTreeCreationTool
{
    internal static class SkillTreeEditorTextInput
    {
        public static string Edit(string value, string input, bool backspace, int maximumLength, Func<char, bool> accepts = null)
        {
            bool replace = false;
            return Edit(value, input, backspace, maximumLength, ref replace, accepts);
        }

        public static string Edit(string value, string input, bool backspace, int maximumLength, ref bool replace, Func<char, bool> accepts = null)
        {
            foreach (char character in input)
            {
                if (accepts != null && !accepts(character))
                    continue;
                if (replace)
                {
                    value = string.Empty;
                    replace = false;
                }
                if (value.Length < maximumLength)
                    value += character;
            }

            if (backspace && value.Length > 0)
            {
                replace = false;
                value = value[..^1];
            }
            return value;
        }
    }
}
