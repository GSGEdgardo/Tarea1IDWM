using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using courses_dotnet_api.Src.DTOs.Account;
using courses_dotnet_api.Src.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace courses_dotnet_api.Src.Controllers;

public class AccountController : BaseApiController
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountRepository _accountRepository;

    public AccountController(IUserRepository userRepository, IAccountRepository accountRepository)
    {
        _userRepository = userRepository;
        _accountRepository = accountRepository;
    }

    [HttpPost("login")]
    [Route("api/account/login")]
    public async Task<IResult> Login(LoginDto loginDto)
    {
        //Dejaré un mensaje predefinido por si las credenciales no coinciden.
        string message = "Credentials are Invalid.";
        var user = await _accountRepository.GetUserByEmailAsync(loginDto.Email);
        //El loginDto va a recibir los datos de la solicitud HTTP que es un Post en este caso
        //Primero debo verificar que el usuario exista o no y para eso tenemos un método en el userRepository
        if (user is null)
        {
            //Si el usuario no existe, le decimos que no se encuentra registrado mediante una bad request
            return Results.BadRequest(message);
        }
        //Ahora que se validó ya que el usuario está guardado en la base de datos, necesitamos su información
        //pero debemos ver el tema de la contraseña, como tenemos la contraseña hasheada y con una sal, hay que 
        //replicarla, y para eso necesitamos usar el HMACSHA512 que es el que se usa en el AccountRepository para añadir la cuenta

        using var hmac = new HMACSHA512(user.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        // Y ahora hay que comparar el Hash obtenido en la solicitud con el de la base de datos
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != user.PasswordHash[i])
            {
                return Results.BadRequest(message);
            }
        }
        
        var email = loginDto.Email;

        //Tecnicamente el AccountDto no debería ser null porque se validó que el correo esté en la base de datos
        //y si está en esta parte del código significa que lo encontró, pero por si acaso se usará el '?' en el AccountDto
        AccountDto? accountDto = await _accountRepository.GetAccountAsync(email);

        //Como se pidió la idea es que si el usuario logea, se envíe el rut, el nombre, el correo y el token.
        return Results.Ok(accountDto);
    }

    [HttpPost("register")]
    public async Task<IResult> Register(RegisterDto registerDto)
    {
        if (
            await _userRepository.UserExistsByEmailAsync(registerDto.Email)
            || await _userRepository.UserExistsByRutAsync(registerDto.Rut)
        )
        {
            return TypedResults.BadRequest("User already exists");
        }

        await _accountRepository.AddAccountAsync(registerDto);

        if (!await _accountRepository.SaveChangesAsync())
        {
            return TypedResults.BadRequest("Failed to save user");
        }

        AccountDto? accountDto = await _accountRepository.GetAccountAsync(registerDto.Email);

        return TypedResults.Ok(accountDto);
    }
}
