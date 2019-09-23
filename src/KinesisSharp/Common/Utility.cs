using System;
using System.Collections.Generic;

namespace KinesisSharp.Common
{
    public class Utility
    {
        public static IList<T> Shuffle<T>(IEnumerable<T> list, Random rnd)
        {
            var result = new List<T>(list);

            for (var i = 0; i < result.Count - 1; i++)
            {
                Swap(result, i, rnd.Next(i, result.Count));
            }

            return result;
        }

        public static void Swap<T>(IList<T> list, int i, int j)
        {
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}