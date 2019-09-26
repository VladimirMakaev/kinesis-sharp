using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace KinesisSharp.Common
{
    public class Empty<T>
    {
        public static readonly ReadOnlyCollection<T> ReadOnlyList = new ReadOnlyCollection<T>(new List<T>(0));
    }
}