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

        /// <summary>
        /// Return all persisted DataEntity rows from the database. Intended for
        /// administrative listing and health checks; avoid using this in high-traffic
        /// code paths without proper paging.
        /// </summary>
        public async Task<IEnumerable<DataEntity>> GetAllAsync()
        {
            var list = await _context.DataEntries.AsNoTracking().ToListAsync();
            return list.Select(e => new DataEntity { Id = e.Id, GuidId = e.GuidId, Value = e.Value, IsDeleted = e.IsDeleted });
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
