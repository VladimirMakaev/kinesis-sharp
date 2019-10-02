namespace KinesisSharp.Leases.Registry.Redis
{
    public static class Keys
    {
        public static string Lease(string application, string shardId)
        {
            return $"L:{application}:{shardId}";
        }

        public static string AllLeases(string application)
        {
            return $"L:{application}:_";
        }
    }
}
