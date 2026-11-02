using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

<<<<<<< Updated upstream
public class AuthServices
=======
public interface IAuthServices
{
    Task<RegisterResponseDto> RegisterUserAsync(RegisterRequestDto authDto);
    Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto,CancellationToken cancellationToken=default);
    Task<bool> ChnagePassword(Guid userId,ChnagePasswordDto chnagePasswordDto);
}

public class AuthServices : IAuthServices
>>>>>>> Stashed changes
{
    private readonly MySqlDbContext _context;

    public AuthServices(MySqlDbContext context)
    {
        _context=context;
    }

<<<<<<< Updated upstream
    // public async Task<AuthDto> RegisterUserAsync(AuthDto authDto)
    // {
    //     var exists= await _context.Users.AnyAsync(u=>u.Email==authDto.Email);
    //     if (exists)
    //     {
    //         return 
    //     }
    // }
=======
    public async Task<bool> ChnagePassword(Guid userId,ChnagePasswordDto chnagePasswordDto)
    {
        
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.UserID==userId);
        if (user == null)
        {
            throw new AppException(StatusCodes.Status404NotFound, "No account found.");
        }
        var hasher = new PasswordHasher<User>();
        if (hasher.VerifyHashedPassword(user, user.PasswordHash,chnagePasswordDto.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            throw new AppException(StatusCodes.Status404NotFound, "Current password is wrong.");
        }
        user.PasswordHash=hasher.HashPassword(user,chnagePasswordDto.NewPassword);
        user.UpdatedAt=DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

>>>>>>> Stashed changes
}
