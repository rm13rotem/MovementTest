namespace Movement.WebApp.Models.DataSources
{
    public interface IDbDataSource : IDataSource
    {
        /// <summary>
        /// Retrieve all entities from the underlying persistent store.
        /// Used by admin/listing UI to enumerate DB contents.
        /// </summary>
        Task<IEnumerable<Movement.WebApp.Models.DataEntity>> GetAllAsync();
    }
}