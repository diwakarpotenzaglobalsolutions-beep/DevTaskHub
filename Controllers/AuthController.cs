using DevTaskHubBackend.Models;
using DevTaskHubBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DevTaskHubBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly IAuthServices _authServices;

        public AuthController(IAuthServices authServices)
        {
            _authServices=authServices;
        }
        
        [AllowAnonymous]
        [HttpPost("Register")]

        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto registerResponseDto)
        {
            var result = await _authServices.RegisterUserAsync(registerResponseDto);

            return Ok(new
            {
                success=true,
                message="User register successfully.",
                data=result
            });
        }
        
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
        {
            var result=await _authServices.LoginAsync(loginDto);

            return Ok(new
            {
                success=true,
                message="Login Successfull.",
                data=result
            });
        }

        [AllowAnonymous]
        [HttpPost("Refresh-Token")]

        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result= await _authServices.RefreshTokenAsync(refreshTokenDto);


            return Ok(new
            {
                success=true,
                message="Refresh token successfully.",
                data=result
            });
        }


    }
}
