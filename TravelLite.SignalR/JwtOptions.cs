using Microsoft.IdentityModel.Tokens;
using System.Text;

public record JwtOptions(string Secret, string Iss, string Aud, int Exp)
{
    internal Lazy<SymmetricSecurityKey> Key = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)));
}
