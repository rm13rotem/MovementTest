namespace Movement.WebApp.Models.DataSources
{
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
        public async Task<DataEntity?> GetAsync(int id)
        {
            var redis = await _redisDataSource.GetAsync(id);
            if (redis != null)
            {
                await _redisDataSource.SetAsync(redis); // Update Redis access time
                return redis;
            }

            var sdcs = await _sdcsDataSource.GetAsync(id);
            if (sdcs != null)
            {
                await _redisDataSource.SetAsync(sdcs);
                return sdcs;
            }

            var db = await _dbDataSource.GetAsync(id);
            if (db != null)
            {
                await _sdcsDataSource.SetAsync(db);
                await _redisDataSource.SetAsync(db);
                return db;
            }

            return null;
        }

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
