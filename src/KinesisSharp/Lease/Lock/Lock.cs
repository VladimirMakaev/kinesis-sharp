using System;

namespace KinesisSharp.Lease.Lock
{
    public class Lock
    {
        public Lock(string lockId, string resource, DateTime expiresOn)
        {
            ExpiresOn = expiresOn;
            LockId = lockId;
            Resource = resource;
        }

        public DateTime ExpiresOn { get; }
        public string LockId { get; }
        public string Resource { get; }

        protected bool Equals(Lock other)
        {
            return ExpiresOn.Equals(other.ExpiresOn) && string.Equals(LockId, other.LockId) &&
                   string.Equals(Resource, other.Resource);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Lock) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ExpiresOn.GetHashCode();
                hashCode = (hashCode * 397) ^ (LockId != null ? LockId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Resource != null ? Resource.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}