using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Helpers
{
    public class JwtSessionValidatorMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtSessionValidatorMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, QLDCContext db)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                await _next(context);
                return;
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            try
            {
                var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
                var idUserClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;

                if (idUserClaim == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Invalid token: missing id_users claim.");
                    return;
                }

                int idUser = int.Parse(idUserClaim);

                bool isValid = await db.JWTSessions
                    .AnyAsync(x => x.id_user == idUser && x.token == token);

                if (!isValid)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Token invalid or logged out.");
                    return;
                }

                await _next(context);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Invalid token format.");
            }
        }
    }
}
