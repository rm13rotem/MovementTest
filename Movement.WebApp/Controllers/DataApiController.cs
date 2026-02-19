using Microsoft.AspNetCore.Mvc;
using Movement.WebApp.Models;
using Movement.WebApp.Models.DataSources;

namespace Movement.WebApp.Controllers
{
    [ApiController]
    [Route("data")]
    public class DataApiController : ControllerBase
    {
        private readonly IDataSource _coordinator;

        public DataApiController(IDataSource coordinator)
        {
            _coordinator = coordinator;
        }

        // GET /data/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DataEntity>> Get(int id)
        {
            var entity = await _coordinator.GetAsync(id);
            if (entity == null)
                return NotFound();
            return Ok(entity);
        }

        // POST /data
        // Accepts a DataEntity payload and returns the assigned Id (int) in the response body.
        [HttpPost]
        public async Task<ActionResult<int>> Post([FromBody] DataEntity model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _coordinator.SetAsync(model);
            if (!success)
                return StatusCode(500, "Failed to save entity");

            // model.Id will be populated by the DB layer when a new record is created.
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model.Id);
        }
    }
}
