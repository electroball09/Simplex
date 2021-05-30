using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Simplex.Util
{
    public static class Jsonutil
    {
        public static JsonElement EvaluateString(this JsonElement elem, string eval)
        {
            var split = eval.Split('.');
            JsonElement next = elem;
            for (int i = 0; i < split.Length; i++)
            {
                string str = split[i];
                if (next.ValueKind == JsonValueKind.Array)
                    next = next[int.Parse(str)];
                else
                    next = next.GetProperty(str);
            }
            return next;
        }

        public static bool GetError(this JsonDocument doc, out string err)
        {
            if (!doc.RootElement.TryGetProperty("error", out var elem))
            {
                err = null;
                return false;
            }

            err = elem.GetRawText();
            return true;
        }
    }
}
