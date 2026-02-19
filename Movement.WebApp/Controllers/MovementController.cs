using Microsoft.AspNetCore.Mvc;
using Movement.WebApp.Models.DataSources;
using Movement.WebApp.Models;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;

namespace Movement.WebApp.Controllers
{
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

        // /Movement/FlushRedis
        public async Task<IActionResult> FlushRedis()
        {
            if (_redisSource == null)
                return BadRequest();

            var ok = await _redisSource.FlushAllAsync();
            if (!ok)
                return StatusCode(500, "Failed to flush Redis");

            return RedirectToAction(nameof(Index));
        }

        // GET: /Movement
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

        // GET: /Movement/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Movement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DataEntity model)
        {
            if (!ModelState.IsValid)
                return View(model);

            await _coordinator.SetAsync(model);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Movement/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _dbSource.GetAsync(id);
            if (entity == null)
                return NotFound();
            return View(entity);
        }

        // POST: /Movement/Edit/5
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

        // POST: /Movement/Delete/5
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

        // POST: /Movement/UnDelete/5
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
