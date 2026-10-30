using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Entities;
using DevTaskHubBackend.Exceptions;
using DevTaskHubBackend.Helpers;
using DevTaskHubBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DevTaskHubBackend.Services;

public interface IAuthServices
{
    Task<RegisterResponseDto> RegisterUserAsync(RegisterRequestDto authDto);
    Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginAsync(LoginDto loginDto,CancellationToken cancellationToken=default);
}

public class AuthServices : IAuthServices
{
    private readonly MySqlDbContext _context;
    private readonly IConfiguration _configuration;


    public AuthServices(MySqlDbContext context,IConfiguration configuration)
    {
        _context = context;
        _configuration=configuration;
    }
    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
          new Claim(ClaimTypes.Name,user.FullName),
          new Claim(ClaimTypes.NameIdentifier,user.UserID.ToString()),
        new Claim(ClaimTypes.Role,RoleHelper.GetRoleName(user.RoleId))
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration.GetValue<string>("AppSettings:Token")!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
        var tokenDescriptor = new JwtSecurityToken(
            issuer: _configuration.GetValue<string>("AppSettings:Issuer"),
            audience: _configuration.GetValue<string>("AppSettings:audience"),
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
    }




        private async Task<string> GenrateAndSaveRefreshToken(User user, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var refreshToken = Convert.ToBase64String(randomNumber);
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(1);
        await _context.SaveChangesAsync(cancellationToken);
        return refreshToken;

    }




        public async Task<LoginResponseDto?> RefreshTokenAsync(RefreshTokenDto request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return null;

        User? user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);
        if (user is null || user.RefreshTokenExpiry <= DateTime.UtcNow)
            return null;

        return new LoginResponseDto
        {
            UserId = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Token = CreateToken(user),
            RefreshToken = await GenrateAndSaveRefreshToken(user, cancellationToken)
        };
    }


        public async Task<RegisterResponseDto> RegisterUserAsync(RegisterRequestDto authDto)
    {
        var email = authDto.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users.AnyAsync(u => u.Email == email);

        if (exists)
        {
            throw new AppException(StatusCodes.Status409Conflict, "Email already registered.");
        }

        var roleId = UserRole.Developer.ToId();

        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == roleId);

        if (role == null)
        {
            throw new AppException(StatusCodes.Status500InternalServerError, "Developer role not found in database.");
        }
        var user = new User
        {
            RoleId = role.RoleId,
            FullName = authDto.FullName.Trim(),
            Email = email,
            PhoneNumber = authDto.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false
        };

        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, authDto.Password);
       await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
        return new RegisterResponseDto
        {
            UserId = user.UserID,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = role.RoleName,
            CreatedAt = user.CreatedAt
        };
    }


    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto,CancellationToken cancellationToken)
    {
        var user=await _context.Users.FirstOrDefaultAsync(u=>u.Email==loginDto.Email);
        if (user == null)
        {
             throw new AppException(StatusCodes.Status404NotFound, "No account found with this email address.");
        }
        if(new PasswordHasher<User>().VerifyHashedPassword(user,user.PasswordHash,loginDto.Password)==PasswordVerificationResult.Failed)
        {
            throw new AppException(StatusCodes.Status404NotFound, "The password you entered is incorrect.");
        }


        return new LoginResponseDto
        {
            UserId=user.UserID,
            FullName=user.FullName,
            Email=user.Email,
            PhoneNumber=user.PhoneNumber,
            Role=RoleHelper.GetRoleName(user.RoleId),
            Token=CreateToken(user),
            RefreshToken=await GenrateAndSaveRefreshToken(user,cancellationToken)
            
        };
        
    }

}
