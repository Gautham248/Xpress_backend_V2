using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RMTController : ControllerBase
    {
        private readonly IRMTServices _rmtRepository;

        public RMTController(IRMTServices rmtRepository)
        {
            _rmtRepository = rmtRepository;
        }
        [HttpGet("project-codes")]
        public async Task<IActionResult> GetProjectCodes()
        {
            var projectCodes = await _rmtRepository.GetAllProjectCodesAsync();
            return Ok(projectCodes);
        }
    }
}
