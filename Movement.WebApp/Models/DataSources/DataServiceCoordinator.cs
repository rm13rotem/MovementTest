namespace Movement.WebApp.Models.DataSources
{
    /// <summary>
    /// Coordinates reads and writes between the three data sources (Redis, SDCS and DB).
    /// Business rules:
    /// - Reads: attempt Redis first, then SDCS, then DB. When a lower-level source is hit,
    ///   promote the result into higher-level caches (SDCS and Redis) so future reads are faster.
    /// - Writes: persist to DB first, then update SDCS, and finally attempt to update Redis.
    /// </summary>
    public class DataServiceCoordinator : IDataSource
    {
        private readonly IRedisDataSource _redisDataSource;
        private readonly ISdcsDataSource _sdcsDataSource;
        private readonly IDbDataSource _dbDataSource;

        public DataServiceCoordinator(IRedisDataSource redisDataSource, ISdcsDataSource sdcsDataSource, IDbDataSource dbDataSource)
        {
            _redisDataSource = redisDataSource;
            _sdcsDataSource = sdcsDataSource;
            _dbDataSource = dbDataSource;
        }

        /// <summary>
        /// Retrieve an entity by id following the cache-coordinator pattern described above.
        /// </summary>
        public async Task<DataEntity?> GetAsync(int id)
        {
            var redis = await _redisDataSource.GetAsync(id);
            if (redis != null)
            {
                // refresh TTL/access in Redis by re-setting the key
                await _redisDataSource.SetAsync(redis);
                return redis;
            }

            var sdcs = await _sdcsDataSource.GetAsync(id);
            if (sdcs != null)
            {
                // promote to Redis for faster subsequent reads
                await _redisDataSource.SetAsync(sdcs);
                return sdcs;
            }

            var db = await _dbDataSource.GetAsync(id);
            if (db != null)
            {
                // persist into caches to warm them
                await _sdcsDataSource.SetAsync(db);
                await _redisDataSource.SetAsync(db);
                return db;
            }

            return null;
        }

        /// <summary>
        /// Persist an entity, following the write-through strategy: DB -> SDCS -> Redis.
        /// Returns true when the operation has been successfully applied to the primary
        /// storage (DB) and the caches were updated.
        /// </summary>
        public async Task<bool> SetAsync(DataEntity dbEntity)
        {
            bool isSuccess = await _dbDataSource.SetAsync(dbEntity);
            if (!isSuccess)
                return false;

            isSuccess = await _sdcsDataSource.SetAsync(dbEntity);
            if (isSuccess)
                return await _redisDataSource.SetAsync(dbEntity);

            return isSuccess;
        }
    }
}
