using Microsoft.AspNetCore.Mvc;
using Movement.WebApp.Models.DataSources;
using Movement.WebApp.Models;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Movement.WebApp.Controllers
{
    /// <summary>
    /// MVC controller used by the UI to inspect and manage DataEntity instances across
    /// the different data sources (Redis, SDCS and DB). It uses injected data source
    /// implementations to perform reads and writes and exposes UI views for CRUD.
    /// </summary>
    public class MovementController : Controller
    {
        private readonly IDataSource _coordinator;
        private readonly IRedisDataSource _redisSource;
        private readonly IDbDataSource _dbSource;
        private readonly ISdcsDataSource _sdcsSource;

        public MovementController(IDataSource coordinator,
                                  IRedisDataSource redisSource,
                                  IDbDataSource dbSource,
                                  ISdcsDataSource sdcsSource)
        {
            _coordinator = coordinator;
            _redisSource = redisSource;
            _dbSource = dbSource;
            _sdcsSource = sdcsSource;
        }

        /// <summary>
        /// Deletes cached keys for this application from Redis. This method attempts to
        /// remove only keys that belong to the configured instance name/prefix so it does
        /// not issue destructive global FLUSH commands.
        /// </summary>
        /// <returns>Redirects back to the Index action.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlushRedis()
        {
            if (_redisSource == null)
                return BadRequest();

            var ok = await _redisSource.FlushAllAsync();
            if (!ok)
                return StatusCode(500, "Failed to flush Redis");

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Index page. When <paramref name="id"/> is provided the action queries the
        /// selected data source for that single entity. When <paramref name="id"/> is
        /// empty the action returns a listing of all entries from the selected source.
        /// </summary>
        /// <param name="id">Optional id to query.</param>
        /// <param name="source">Source to query: "redis", "sdcs", "db" or "coordinator".</param>
        /// <returns>Renders the index view with the requested data.</returns>
        public async Task<IActionResult> Index(int? id, string source = "redis")
        {
            var model = new MovementIndexViewModel
            {
                SelectedSource = source,
                QueryId = id
            };

            // If no id provided show current entries from the selected source
            if (!id.HasValue)
            {
                try
                {
                    switch (source?.ToLowerInvariant())
                    {
                        case "redis":
                            model.RedisEntries = (await _redisSource.GetAllAsync()).ToList();
                            break;
                        case "sdcs":
                            model.RedisEntries = (await _sdcsSource.GetAllAsync()).ToList();
                            break;
                        case "db":
                            model.RedisEntries = (await _dbSource.GetAllAsync()).ToList();
                            break;
                        default:
                        case "coordinator":
                            // coordinator doesn't expose GetAllAsync — fallback to DB
                            model.RedisEntries = (await _dbSource.GetAllAsync()).ToList();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                return View(model);
            }

            // id provided: choose source
            DataEntity? entity = null;
            switch (source?.ToLowerInvariant())
            {
                case "redis":
                    entity = await _redisSource.GetAsync(id.Value);
                    break;
                case "sdcs":
                    entity = await _sdcsSource.GetAsync(id.Value);
                    break;
                case "db":
                    entity = await _dbSource.GetAsync(id.Value);
                    break;
                default:
                case "coordinator":
                    entity = await _coordinator.GetAsync(id.Value);
                    break;
            }

            model.SelectedEntity = entity;
            return View(model);
        }

        /// <summary>
        /// Shows the create form for a new DataEntity.
        /// </summary>
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Persist a new DataEntity (or update existing). Uses the coordinator to
        /// write through to DB and caches.
        /// </summary>
        /// <param name="model">The entity to create.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DataEntity model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _coordinator.SetAsync(model);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Show the edit form for an existing entity. Reads from the DB source.
        /// </summary>
        /// <param name="id">Entity id.</param>
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _dbSource.GetAsync(id);
            if (entity == null)
                return NotFound();
            return View(entity);
        }

        /// <summary>
        /// Save edited entity values.
        /// </summary>
        /// <param name="id">Entity id (route).</param>
        /// <param name="model">Posted entity model.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DataEntity model)
        {
            if (id != model.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(model);

            await _coordinator.SetAsync(model);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Soft-delete an entity by setting <see cref="DataEntity.IsDeleted"/> and
        /// persisting the change through the coordinator.
        /// </summary>
        /// <param name="id">Entity id.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _dbSource.GetAsync(id);
            if (entity == null)
                return NotFound();
            entity.IsDeleted = true;
            await _coordinator.SetAsync(entity);
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Undo a soft-delete on an entity.
        /// </summary>
        /// <param name="id">Entity id.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnDelete(int id)
        {
            var entity = await _dbSource.GetAsync(id);
            if (entity == null)
                return NotFound();
            entity.IsDeleted = false;
            await _coordinator.SetAsync(entity);
            return RedirectToAction(nameof(Index));
        }
    }
}
