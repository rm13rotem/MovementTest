using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Movement.WebApp.Models.DataSources;

namespace Movement.WebApp.Models.SelfDeterminedCacheSystem
{
    public class SelfDesignedCache : ISdcsDataSource
    {
        // private members
        private readonly Dictionary<int, CacheItem<string>> _cache;
        private int _capacity = 10; 
        public int Capacity
        {
            get
            {
                if (_capacity < 3 || _capacity > 100)
                    _capacity = 10;
                return _capacity;
            }
   set
            {
                if (value < 3 || value > 100)
                    throw new ArgumentException("Capacity must be between 3 and 100");
                else _capacity = value;
            }
        }

        // ctor
        public SelfDesignedCache(int capacity = 10)
        {
            Capacity = capacity;
            _cache = new Dictionary<int, CacheItem<string>>();
        }


        public Task<IEnumerable<DataEntity>> GetAllAsync()
        {
            var list = _cache.Select(kvp => new DataEntity { Id = kvp.Key, Value = kvp.Value.Value }).ToList();
            return Task.FromResult<IEnumerable<DataEntity>>(list);
        }


        /// <summary>
        /// Add or update an item in the self-designed cache. Uses a simple Least-Recently-Used
        /// eviction when capacity is exceeded. This cache is intended for local application
        /// acceleration and is not persisted.
        /// </summary>
        public async Task<bool> SetAsync(DataEntity dataEntity)
        {
            try
            {
                if (_cache.Count >= Capacity)
                {
                    RemoveLeastUsed();
                }

                _cache[dataEntity.Id] = new CacheItem<string>
                {
                    Value = dataEntity.Value,
                    LastAccessed = DateTime.UtcNow
                };
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }

        }

        /// <summary>
        /// Retrieve an item from the cache. Updates the last-accessed timestamp for LRU behavior.
        /// </summary>
        public async Task<DataEntity?> GetAsync(int id)
        {
            try
            {
                if (_cache.TryGetValue(id, out var item))
                {
                    DataEntity dataEntity = new DataEntity
                    {
                        Id = id,
                        Value = item.Value
                    };
                    // item is CacheItem<string>
                    item.LastAccessed = DateTime.UtcNow;
                    return dataEntity;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return null;
            }
        }

        private void RemoveLeastUsed()
        {
            var leastUsed = _cache
                .OrderBy(x => x.Value.LastAccessed)
                .First();

            _cache.Remove(leastUsed.Key);
        }

    }

}
