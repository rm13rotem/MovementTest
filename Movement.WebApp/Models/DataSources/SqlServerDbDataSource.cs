using Microsoft.EntityFrameworkCore;
using Movement.WebApp.Models;

namespace Movement.WebApp.Models.DataSources
{
    public class SqlServerDbDataSource : IDbDataSource
    {
        private readonly MovementEntities _context;

        public SqlServerDbDataSource(MovementEntities context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DataEntity>> GetAllAsync()
        {
            return await _context.DataEntries.ToListAsync();
        }

        public async Task<DataEntity?> GetAsync(int id)
        {
            var entry = await _context.DataEntries.FirstOrDefaultAsync(e => e.Id == id);
            if (entry == null)
                return null;

            return new DataEntity
            {
                Id = entry.Id,
                GuidId = entry.GuidId,
                Value = entry.Value
            };
        }

        public async Task<bool> SetAsync(DataEntity entity)
        {
            try
            {
                var entry = await _context.DataEntries.FirstOrDefaultAsync(e => e.Id == entity.Id);
                if (entry == null)
                {
                    entry = new DataEntity
                    {
                        GuidId = entity.GuidId,
                        Value = entity.Value
                    };
                    _context.DataEntries.Add(entry);
                }
                else
                {
                    entry.Value = entity.Value;
                    entry.GuidId = entity.GuidId;
                    _context.DataEntries.Update(entry);
                }

                await _context.SaveChangesAsync();

                entity.Id = entry.Id;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
    }
}
