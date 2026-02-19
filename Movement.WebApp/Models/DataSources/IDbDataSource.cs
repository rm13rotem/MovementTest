namespace Movement.WebApp.Models.DataSources
{
    public interface IDbDataSource : IDataSource
    {
        Task<IEnumerable<Movement.WebApp.Models.DataEntity>> GetAllAsync();
    }
}