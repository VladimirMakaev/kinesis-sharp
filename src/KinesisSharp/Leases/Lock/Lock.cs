using System;

namespace KinesisSharp.Leases.Lock
{
    public class Lock
    {
        public Lock(string lockId, string resource, DateTime expiresOn, string ownerId)
        {
            ExpiresOn = expiresOn;
            OwnerId = ownerId;
            LockId = lockId;
            Resource = resource;
        }

        public DateTime ExpiresOn { get; }
        public string OwnerId { get; }
        public string LockId { get; }
        public string Resource { get; }

        protected bool Equals(Lock other)
        {
            return ExpiresOn.Equals(other.ExpiresOn) && string.Equals(OwnerId, other.OwnerId) &&
                   string.Equals(LockId, other.LockId) && string.Equals(Resource, other.Resource);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((Lock)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ExpiresOn.GetHashCode();
                hashCode = (hashCode * 397) ^ (OwnerId != null ? OwnerId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LockId != null ? LockId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Resource != null ? Resource.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
