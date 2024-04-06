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
        //se retorna un objeto de tipo PasswordDto que contiene la sal y el hash de la contraseña gracias al Dto
        var passwordHashSaltDto = await _accountRepository.GetPasswordByEmailAsync(loginDto.Email);
        
        //El loginDto va a recibir los datos de la solicitud HTTP que es un Post en este caso
        //Primero debo verificar que el usuario exista o no y para eso tenemos un método en el
        //userRepository que utiliza el LoginDto
        if (!await _userRepository.UserExistsByEmailAsync(loginDto.Email))
        {
            //Si el usuario no existe, le decimos que no se encuentra registrado mediante una bad request
            return Results.BadRequest(message);
        }

        //Una vez que verificamos que el usuario está en la base de datos con el if anterior,
        //necesitamos comparar la contraseña que se envió en la solicitud con la que está en la base de datos
        //Para esto gracias a nuestro PasswordDto tenemos la sal y el hash de la contraseña, ahora hay que hacer 
        //el proceso de hasheo de la contraseña que se envió en la solicitud para compararla con la de la base de datos

        using var hmac = new HMACSHA512(passwordHashSaltDto.PasswordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

        // Y ahora hay que comparar el Hash obtenido en la solicitud con el de la base de datos
        for (int i = 0; i < computedHash.Length; i++)
        {
            if (computedHash[i] != passwordHashSaltDto.PasswordHash[i])
            {
                return Results.BadRequest(message);
            }
        }

        //Tecnicamente el AccountDto no debería ser null porque se validó que el correo esté en la base de datos
        //y si está en esta parte del código significa que lo encontró, pero por si acaso se usará el '?' en el AccountDto
        //Esta linea a continuación se usa para poder obtener los datos que después se van a desplegar por consola al iniciar sesión
        AccountDto? accountDto = await _accountRepository.GetAccountAsync(loginDto.Email);

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
