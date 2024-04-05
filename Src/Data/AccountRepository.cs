using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using courses_dotnet_api.Src.DTOs.Account;
using courses_dotnet_api.Src.Interfaces;
using courses_dotnet_api.Src.Models;
using Microsoft.EntityFrameworkCore;

namespace courses_dotnet_api.Src.Data;

public class AccountRepository : IAccountRepository
{
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;

    public AccountRepository(DataContext dataContext, IMapper mapper, ITokenService tokenService)
    {
        _dataContext = dataContext;
        _mapper = mapper;
        _tokenService = tokenService;
    }

    public async Task AddAccountAsync(RegisterDto registerDto)
    {
        using var hmac = new HMACSHA512();

        User user =
            new()
            {
                Rut = registerDto.Rut,
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

        await _dataContext.Users.AddAsync(user);
    }

    public async Task<AccountDto?> GetAccountAsync(string email)
    {
        User? user = await _dataContext
            .Users.Where(student => student.Email == email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        AccountDto accountDto =
            new()
            {
                Rut = user.Rut,
                Name = user.Name,
                Email = user.Email,
                Token = _tokenService.CreateToken(user.Rut)
            };

        return accountDto;
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        //La idea es poder obtener al usuario por su correo electrónico, entonces cuando lo
        //tengo poder devolverlo por completo, no se si está correcto porque la idea del Dto es enviar
        //lo que necesito, yo ahora necesito la contraseña para validarla, pero no se si es mejor obtenerla así o enviarla
        //y devolver un booleano al controlador diciendo si la contraseña está bien o no
        User? user = await _dataContext
            .Users.Where(student => student.Email == email)
            .FirstOrDefaultAsync();

        return user;
    }


    public async Task<bool> SaveChangesAsync()
    {
        return 0 < await _dataContext.SaveChangesAsync();
    }
}
