using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;

namespace Xpress_backend_V2.Controllers
{
    [ApiController]
    [Authorize(Roles = "Project Manager,Vice President,Admin")] 
    [Route("api/[controller]")]

    public class ProjectRoleController : ControllerBase
    {
        private readonly IProjectRoleService _projectRoleService;

        public ProjectRoleController(IProjectRoleService projectRoleService)
        {
            _projectRoleService = projectRoleService;
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmailProjects([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            try
            {
                var projects = await _projectRoleService.GetProjectsForEmailAsync(email);
                if (projects == null || projects.Count == 0)
                {
                    return NotFound("No active projects found where the user is a Project Manager or DU Head.");
                }

                return Ok(projects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}