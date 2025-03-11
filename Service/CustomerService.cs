using Inventree_App.Context;
using Inventree_App.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Inventree_App.Service
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationContext _context;
        private readonly IConfiguration _configuration;
        public CustomerService(ApplicationContext context, IConfiguration configuration) {
            _configuration = configuration;
            _context = context;
        }

        public string GenerateJwtToken(Customer user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim("Name", user.FirstName),
            new Claim("UserId", user.Id.ToString())
        };

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpiryMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
