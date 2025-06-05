using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;
using Xpress_backend_V2.Services.Interface;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService authService;
        private readonly IUserRepository userRepository;

        public AuthController(IAuthService authService, IUserRepository userRepository)
        {
            this.authService = authService;
            this.userRepository = userRepository;
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] LoginDTO loginDTO)
        {

            var loggedInUser = await userRepository.LoginUser(loginDTO.Email, loginDTO.Password);

            if (loggedInUser == null)
            {
                return NotFound("User Not Found");
            }
            var jwt = authService.GenerateToken(loggedInUser.UserId, loggedInUser.EmployeeEmail, loggedInUser.UserRole);
            return Ok(new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = loggedInUser.UserId,
                user_name = loggedInUser.EmployeeName,
                role_name = loggedInUser.UserRole

            });

        }
    }
}
