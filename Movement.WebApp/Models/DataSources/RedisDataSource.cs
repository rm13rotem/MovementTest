using Microsoft.Extensions.Configuration;

namespace Movement.WebApp.Models.DataSources        
{
    public class RedisDataSource : IRedisDataSource
    {
        private readonly StackExchange.Redis.IConnectionMultiplexer _multiplexer;
        private readonly string _instanceName;
    
        private const string KeyPrefix = "DataEntity:";

        /// <summary>
        /// Create a new Redis-backed data source.
        /// </summary>
        /// <param name="multiplexer">StackExchange.Redis connection multiplexer (injected).</param>
        /// <param name="config">Application configuration (used to read Redis:InstanceName).</param>
        public RedisDataSource(StackExchange.Redis.IConnectionMultiplexer multiplexer, IConfiguration config)
        {
            _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
            _instanceName = config.GetSection("Redis:InstanceName").Value ?? string.Empty;
        }

        /// <summary>
        /// Return all entries matching the application's instance key prefix in Redis.
        /// Note: key scanning may be slow on large datasets and requires access to server
        /// endpoints (server.Keys). For production systems prefer maintaining an index set
        /// of keys instead of scanning.
        /// </summary>
        public async Task<IEnumerable<DataEntity>> GetAllAsync()
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var endpoints = _multiplexer.GetEndPoints();
                var result = new List<DataEntity>();
                if (endpoints.Length == 0)
                    return result;

                var server = _multiplexer.GetServer(endpoints[0]);
                var pattern = string.IsNullOrEmpty(_instanceName) ? "DataEntity:*" : _instanceName + ":DataEntity:*";
                var keys = server.Keys(pattern: pattern).Take(1000);
                foreach (var key in keys)
                {
                    var v = await db.StringGetAsync(key).ConfigureAwait(false);
                    if (!v.IsNullOrEmpty)
                    {
                        var parts = key.ToString().Split(':');
                        if (int.TryParse(parts.Last(), out var parsedId))
                        {
                            result.Add(new DataEntity { Id = parsedId, Value = v });
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return Enumerable.Empty<DataEntity>();
            }
        }

        /// <summary>
        /// Remove all application-scoped keys from Redis. This implementation deletes only
        /// keys that match the configured instance prefix instead of issuing FLUSHALL so it
        /// works on managed Redis instances where admin operations may be disabled.
        /// </summary>
        public async Task<bool> FlushAllAsync()
        {
            // Safer implementation: delete only keys that match our application prefix
            // instead of issuing FLUSHALL which may be unavailable or too destructive
            try
            {
                var endpoints = _multiplexer.GetEndPoints();
                var db = _multiplexer.GetDatabase();
                if (endpoints.Length == 0)
                    return false;

                var pattern = string.IsNullOrEmpty(_instanceName) ? "DataEntity:*" : _instanceName + ":DataEntity:*";

                foreach (var ep in endpoints)
                {
                    var server = _multiplexer.GetServer(ep);
                    if (server == null || !server.IsConnected)
                        continue;

                    var keys = new List<StackExchange.Redis.RedisKey>();
                    // enumerate keys (SCAN under the hood) and collect them
                    foreach (var key in server.Keys(pattern: pattern))
                    {
                        keys.Add(key);
                        // batch deletes in chunks
                        if (keys.Count >= 500)
                        {
                            await db.KeyDeleteAsync(keys.ToArray()).ConfigureAwait(false);
                            keys.Clear();
                        }
                    }

                    if (keys.Count > 0)
                    {
                        await db.KeyDeleteAsync(keys.ToArray()).ConfigureAwait(false);
                        keys.Clear();
                    }
                }

                return true;
            }
            catch (StackExchange.Redis.RedisCommandException rcx)
            {
                // Common when admin commands are disabled (e.g., managed Redis)
                Console.WriteLine("Redis command error while flushing: " + rcx.Message);
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        private string MakeKey(int id)
        {
            if (string.IsNullOrEmpty(_instanceName))
                return KeyPrefix + id;
            return _instanceName + ":" + KeyPrefix + id;
        }

        /// <summary>
        /// Read a single entity from Redis by id. Returns null when the key is absent.
        /// </summary>
        public async Task<DataEntity?> GetAsync(int id)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var key = MakeKey(id);
                var value = await db.StringGetAsync(key).ConfigureAwait(false);
                if (value.IsNullOrEmpty)
                    return null;

                return new DataEntity
                {
                    Id = id,
                    Value = value
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Store an entity in Redis with a 5-minute TTL. Writes are idempotent and will
        /// overwrite existing values for the same key.
        /// </summary>
        public async Task<bool> SetAsync(DataEntity entity)
        {
            try
            {
                var db = _multiplexer.GetDatabase();
                var key = MakeKey(entity.Id);
                // Set with a 5-minute TTL so cached items expire if not refreshed.
                return await db.StringSetAsync(key, entity.Value, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
