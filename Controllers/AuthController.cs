using DevTaskHubBackend.Data;
using DevTaskHubBackend.Helpers;
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
            _authServices = authServices;
        }

        [AllowAnonymous]
        [HttpPost("Register")]

        public async Task<IActionResult> RegisterAsync([FromBody] RegisterRequestDto registerResponseDto)
        {
            var result = await _authServices.RegisterUserAsync(registerResponseDto);

            return Ok(new
            {
                success = true,
                message = "User register successfully.",
                data = result
            });
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginDto loginDto)
        {
            var result = await _authServices.LoginAsync(loginDto);

            return Ok(new
            {
                success = true,
                message = "Login Successfull.",
                data = result
            });
        }

        [AllowAnonymous]
        [HttpPost("Refresh-Token")]

        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            var result = await _authServices.RefreshTokenAsync(refreshTokenDto);


            return Ok(new
            {
                success = true,
                message = "Refresh token successfully.",
                data = result
            });
        }

        [Authorize]
        [HttpPost("Chnage-Password")]
        public async Task<IActionResult> ChnagePassword([FromBody] ChnagePasswordDto chnagePasswordDto)
        {
            var userId = User.GetUserId();

            var result = await _authServices.ChnagePassword(userId, chnagePasswordDto);

            Console.WriteLine(result);

            return Ok(new
            {
                status = true,
                message = "Chnage password successfull."
            });
        }

        [Authorize]
        [HttpGet("Get-Profile-Details")]
        public async Task<IActionResult> GetProfileDetails()
        {
            var userId = User.GetUserId();
            var result = await _authServices.GetProfileDetails(userId);
            return Ok(new
            {
                status = true,
                message = "Get profile details successfull.",
                data = result
            });
        }

        [RoleAuthorize(UserRole.Admin)]
        [HttpPost("ActiveAndDeActiveUser-User")]

        public async Task<IActionResult> ActiveAndDeActiveUser(ActiveAndDeActiveUserDto activeAndDeActiveUserDto)
        {

            Guid currentId = User.GetUserId();

            if (currentId == Guid.Empty)
            {
                return Unauthorized();
            }
            var result = await _authServices.ActiveAndDeActiveUserAsync(activeAndDeActiveUserDto);

            return Ok(new
            {
                Success = result,
                Message = activeAndDeActiveUserDto.Active == 0 ? "User deactivated successfully." : "User actived successfully."
            });
        }

        [Authorize]
        [HttpPost("Edit-Profile")]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileDto request)
        {
            Guid userId = User.GetUserId();

            if (userId == Guid.Empty)
            {
                return Unauthorized(new
                {
                    Success = false,
                    Message = "Unauthorized access."
                });
            }
            var result = await _authServices.EditProfileAsync(userId, request);

            return Ok(new
            {
                Success = true,
                Message = "Profile updated successfully.",
                Data = result
            });

        }
        [HttpPut("Assign-Developer")]
        [RoleAuthorize(UserRole.Admin)]
        public async Task<IActionResult> AssignDeveloper([FromBody] AssignDevloperDto request, CancellationToken cancellationToken)
        {
            await _authServices.AssignDevloperToManagerAsync(request, cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Developer assigned to Project Manager successfully."
            });
        }

        [HttpGet("My-developers")]
        [RoleAuthorize(UserRole.Admin, UserRole.Manager)]
        public async Task<IActionResult> GetMyDevelopers(
        CancellationToken cancellationToken)
        {
            Guid managerId = User.GetUserId();

            if (managerId == Guid.Empty)
                return Unauthorized();

            var developers = await _authServices.GetMyDevelopersAsync(
                managerId,
                cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Developers retrieved successfully.",
                Data = developers
            });
        }

        [HttpGet("Manager-Developers")]
        [RoleAuthorize(UserRole.Admin)]
        public async Task<IActionResult> GetManagerDevelopers(CancellationToken cancellationToken)
        {
            var result = await _authServices.GetManagerDevelopersAsync(
                cancellationToken);

            return Ok(new
            {
                Success = true,
                Message = "Manager and developer details retrieved successfully.",
                Data = result
            });
        }

    }


}
