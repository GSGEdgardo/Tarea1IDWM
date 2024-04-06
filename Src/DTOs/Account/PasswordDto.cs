using System.ComponentModel.DataAnnotations;

namespace courses_dotnet_api.Src.DTOs.Account;

public class PasswordDto
{
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
}