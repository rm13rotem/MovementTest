namespace Movement.WebApp.Models.DataSources
{
    public interface ISdcsDataSource : IDataSource
    {
        Task<IEnumerable<Movement.WebApp.Models.DataEntity>> GetAllAsync();
    }
}