using Microsoft.AspNetCore.Mvc;
using Movement.WebApp.Models;
using Movement.WebApp.Models.DataSources;

namespace Movement.WebApp.Controllers
{
    /// <summary>
    /// Web API controller that exposes data operations for <see cref="DataEntity"/>.
    /// Uses the configured <see cref="IDataSource"/> (the coordinator) to perform
    /// retrieval and persistence. Designed for machine-to-machine usage (JSON API).
    ///
    /// Documentation notes (business-critical):
    /// - The POST endpoint returns the integer id assigned by the primary data store (DB).
    /// - The coordinator implements the write-through strategy so the returned id is
    ///   guaranteed to be persisted in the DB when POST returns a successful response.
    /// </summary>
    [ApiController]
    [Route("data")]
    public class DataApiController : ControllerBase
    {
        private readonly IDataSource _coordinator;

        public DataApiController(IDataSource coordinator)
        {
            _coordinator = coordinator;
        }

        /// <summary>
        /// Retrieve a data entity by identifier.
        /// </summary>
        /// <param name="id">Identifier of the requested <see cref="DataEntity"/>.</param>
        /// <returns>
        /// 200 (OK) with the <see cref="DataEntity"/> when found; 404 (NotFound) otherwise.
        /// </returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<DataEntity>> Get(int id)
        {
            var entity = await _coordinator.GetAsync(id);
            if (entity == null)
                return NotFound();
            return Ok(entity);
        }

        /// <summary>
        /// Persist a <see cref="DataEntity"/> instance. When a new entity is created,
        /// the resulting database identifier is returned in the response body.
        /// </summary>
        /// <param name="model">The <see cref="DataEntity"/> to save. When creating new entities,
        /// the <see cref="DataEntity.Id"/> value may be ignored by the server and will be
        /// populated on success.</param>
        /// <returns>
        /// 201 (Created) with the assigned integer id in the response body on success,
        /// 400 (Bad Request) on validation failure, or 500 (Server Error) on persistence failure.
        /// </returns>
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
