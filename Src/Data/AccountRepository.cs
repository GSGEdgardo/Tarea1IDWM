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

    //Para obtener la contraseña de un usuario por su correo electrónico se creó un Dto de contraseña que trae
    //El passwordSalt y el passwordHash
    public async Task<PasswordDto> GetPasswordByEmailAsync(string email)
    {
        //Se busca el usuario por su correo electrónico y se obtiene el primero que cumpla con la condición
        var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            return null;
        }
        //En caso de encontrarlo se mapea el usuario a un PasswordDto y se retorna para usarlo en el controlador
        var passwordDto = _mapper.Map<PasswordDto>(user);
        return passwordDto;
    }
    public async Task<bool> SaveChangesAsync()
    {
        return 0 < await _dataContext.SaveChangesAsync();
    }
}
