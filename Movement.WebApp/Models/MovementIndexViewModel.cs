namespace Movement.WebApp.Models
{
    public class MovementIndexViewModel
    {
        public string SelectedSource { get; set; } = "redis";
        public int? QueryId { get; set; }
        public DataEntity? SelectedEntity { get; set; }
        public List<DataEntity>? RedisEntries { get; set; }
    }
}
