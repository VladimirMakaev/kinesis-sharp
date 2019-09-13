using System;

namespace KinesisSharp.Lease
{
    public class Lease
    {
        public string Owner { get; }

        public string LeaseId { get; set; }

        public string LeaseCounter { get; set; }

        public Guid? ConcurrencyToken { get; set; }

        public SequenceNumber Checkpoint { get; }

        public SequenceNumber PendingCheckpoint { get; set; }
    }
}