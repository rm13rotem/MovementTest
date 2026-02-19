namespace Movement.WebApp.Models.DataSources
{
    public interface ISdcsDataSource : IDataSource
    {
        /// <summary>
        /// Return all entries currently held in the self-designed cache. This method is intended
        /// for administrative UI and diagnostics.
        /// </summary>
        Task<IEnumerable<Movement.WebApp.Models.DataEntity>> GetAllAsync();
    }
}