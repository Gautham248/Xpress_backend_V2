using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xpress_backend_V2.Interface;
using Xpress_backend_V2.Models.DTO;

namespace Xpress_backend_V2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {


        private IUserRepository userRepository;
        public UserController(IUserRepository userRepository)
        {
            this.userRepository = userRepository;

        }

 
        [HttpPost("register")]
        public async Task<ActionResult> Register([FromBody] UserRegisterDTO user)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                return BadRequest(new { errors });
            }

            var registeredUser = await userRepository.RegisterUser(user);
            if (registeredUser != null)
            {
                return Ok(registeredUser);
            }
            else
            {
                return Conflict("User with this email already exists.");
            }
        }

    }
}
