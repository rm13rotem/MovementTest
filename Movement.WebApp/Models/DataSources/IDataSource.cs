namespace Movement.WebApp.Models.DataSources
{
    public interface IDataSource
    {
        Task<DataEntity?> GetAsync(int id);
        Task<bool> SetAsync(DataEntity entity);
    }

}
