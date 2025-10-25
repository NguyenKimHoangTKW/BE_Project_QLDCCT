using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ProjectQLDCCT.Controllers.Login
{
    [Route("api")]
    [ApiController]
    public class LoginApi : ControllerBase
    {
        private readonly QLDCContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _config;
        public LoginApi(QLDCContext context, JwtHelper jwtHelper, IConfiguration config)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _config = config;
        }
        [HttpPost]
        [Route("login-with-google")]
        public async Task<IActionResult> LoginWithGoogle([FromBody] LoginDTOs items)
        {
            if (items == null || string.IsNullOrWhiteSpace(items.email))
                return BadRequest("Payload không hợp lệ.");

            var nowUnix = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var user = await _context.Users.SingleOrDefaultAsync(x => x.email == items.email);
            if (user == null)
            {
                user = new User
                {
                    Username = items.Username,
                    email = items.email,
                    avatar_url = items.avatar_url,
                    time_cre = nowUnix,
                    time_up = nowUnix,
                    id_type_users = 1,
                    status = 1
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                user.Username = items.Username;
                user.avatar_url = items.avatar_url;
                user.time_up = nowUnix;
                await _context.SaveChangesAsync();
            }
            var jwtSection = _config.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSection["Key"]);
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiresMinutes = double.Parse(jwtSection["ExpiresMinutes"]);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim("id_users", user.id_users.ToString()),
                new Claim("email", user.email ?? "")
            }),
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            var nowUtc = DateTime.UtcNow;
            var expiredSessions = await _context.JWTSessions
                .Where(s => s.id_user == user.id_users && s.ExpiresAt <= nowUtc)
                .ToListAsync();

            if (expiredSessions.Count > 0)
                _context.JWTSessions.RemoveRange(expiredSessions);

            var jwtSession = new JWTSession
            {
                id_user = user.id_users,
                token = jwt,
                CreatedAt = nowUtc,
                ExpiresAt = nowUtc.AddMinutes(expiresMinutes),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                DeviceName = Request.Headers["User-Agent"].ToString()
            };
            _context.JWTSessions.Add(jwtSession);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("jwt", jwt, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes)
            });

            return Ok(new
            {
                success = true,
                message = "Đăng nhập thành công",
                user = new
                {
                    user.id_users,
                    user.Username,
                    user.email,
                    user.id_type_users,
                    user.avatar_url
                }
            });
        } 
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { success = false, message = "Không tìm thấy token trong cookie." });

            var session = await _context.JWTSessions.FirstOrDefaultAsync(x => x.token == token);
            _context.JWTSessions.Remove(session);
            await _context.SaveChangesAsync();

            Response.Cookies.Delete("jwt", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(-1)
            });
            return Ok(new
            {
                success = true,
                message = "Đăng xuất thành công."
            });
        }

    }
}
