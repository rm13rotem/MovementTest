namespace Movement.WebApp.Models.DataSources
{
    public interface IRedisDataSource : IDataSource
    {
        Task<IEnumerable<Movement.WebApp.Models.DataEntity>> GetAllAsync();
        Task<bool> FlushAllAsync();
    }
}