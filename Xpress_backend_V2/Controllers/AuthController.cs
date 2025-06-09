using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models;
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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(APIResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(APIResponse))]
        public async Task<ActionResult<APIResponse>> Login([FromBody] LoginDTO loginDTO)
        {
            var apiResponse = new APIResponse();

            if (string.IsNullOrWhiteSpace(loginDTO.Email) || string.IsNullOrWhiteSpace(loginDTO.Password))
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.BadRequest;
                apiResponse.ErrorMessages.Add("Email and password must not be empty.");
                return BadRequest(apiResponse);
            }

            var loggedInUser = await userRepository.LoginUser(loginDTO.Email, loginDTO.Password);

            if (loggedInUser == null)
            {
                apiResponse.IsSuccess = false;
                apiResponse.StatusCode = HttpStatusCode.NotFound;
                apiResponse.ErrorMessages.Add("User not found or invalid credentials.");
                return NotFound(apiResponse);
            }

            var jwt = authService.GenerateToken(
                loggedInUser.UserId,
                loggedInUser.EmployeeEmail,
                loggedInUser.UserRole
            );

            apiResponse.IsSuccess = true;
            apiResponse.StatusCode = HttpStatusCode.OK;
            apiResponse.Result = new
            {
                access_token = jwt,
                token_type = "bearer",
                user_id = loggedInUser.UserId,
                user_name = loggedInUser.EmployeeName,
                role_name = loggedInUser.UserRole,
                user_email=loggedInUser.EmployeeEmail,
                user_du=loggedInUser.Department

            };

            return Ok(apiResponse);
        }


    }
}
