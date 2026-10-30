using System;
using DevTaskHubBackend.Data;
using DevTaskHubBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace DevTaskHubBackend.Services;

public class AuthServices
{
    private readonly MySqlDbContext _context;

    public AuthServices(MySqlDbContext context)
    {
        _context=context;
    }

    // public async Task<AuthDto> RegisterUserAsync(AuthDto authDto)
    // {
    //     var exists= await _context.Users.AnyAsync(u=>u.Email==authDto.Email);
    //     if (exists)
    //     {
    //         return 
    //     }
    // }
}
