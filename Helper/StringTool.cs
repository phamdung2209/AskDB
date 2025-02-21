﻿using Markdig;

namespace Helper
{
    public static class StringTool
    {
        public static async Task<List<string>> GetLines(string path, bool useDecoding = false)
        {
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);

                if (lines.Length == 0)
                {
                    return [];
                }

                if (useDecoding)
                {
                    return lines.AsParallel().Select(StringCipher.Decode).ToList();
                }
                else
                {
                    return [.. lines];
                }
            }

            return [];
        }

        public static bool IsNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            {
                return true;
            }

            return false;
        }

        public static string AsPlainText(string markdown)
        {
            return Markdown.ToPlainText(markdown).Trim();
        }
    }
}
