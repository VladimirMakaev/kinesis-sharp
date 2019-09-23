using System.Collections.Generic;

namespace KinesisSharp.Leases
{
    public class LeaseMatchingResult
    {
        public LeaseMatchingResult(IReadOnlyCollection<Lease> leasesToBeCreated,
            IReadOnlyCollection<Lease> leasesToBeDeleted)
        {
            LeasesToBeCreated = leasesToBeCreated;
            LeasesToBeDeleted = leasesToBeDeleted;
        }

        public IReadOnlyCollection<Lease> LeasesToBeCreated { get; }

        public IReadOnlyCollection<Lease> LeasesToBeDeleted { get; }
    }
}