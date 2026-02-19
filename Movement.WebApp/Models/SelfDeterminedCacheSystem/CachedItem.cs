namespace Movement.WebApp.Models.SelfDeterminedCacheSystem
{
    class CacheItem<T>
    {
        public T Value { get; set; }
        public DateTime LastAccessed { get; set; }
    }
}
