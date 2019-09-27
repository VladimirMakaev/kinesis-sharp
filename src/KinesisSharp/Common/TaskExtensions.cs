using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KinesisSharp.Common
{
    static class TaskExtensions
    {
        public static readonly Task Completed = Task.FromResult(0);

        public static void Ignore(this Task task)
        {
            // ignored
        }
    }
}
