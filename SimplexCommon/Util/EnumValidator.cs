using System;
using System.Collections.Generic;
using System.Text;

namespace Simplex.Util
{
    public static class EnumValidator<T> where T : Enum
    {
        static List<T> valuesList = new List<T>();

        public static bool IsValid(T value)
        {
            if (valuesList.Count == 0)
                PopulateValues();

            return valuesList.Contains(value);
        }

        private static void PopulateValues()
        {
            var arr = Enum.GetValues(typeof(T));
            foreach (var obj in arr)
                valuesList.Add((T)obj);
        }
    }
}
