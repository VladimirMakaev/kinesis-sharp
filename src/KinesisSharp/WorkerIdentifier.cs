using System;

namespace KinesisSharp
{
    public class WorkerIdentifier
    {
        private WorkerIdentifier(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public static WorkerIdentifier New()
        {
            return new WorkerIdentifier("workerId-" + Guid.NewGuid().ToString("N"));
        }
    }
}