using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;

namespace ProjectQLDCCT.Helpers
{
    public class JwtSessionMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, QLDCContext db)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring(7).Trim();
                var session = await db.JWTSessions.FirstOrDefaultAsync(x => x.token == token);

                if (session == null)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Phiên đăng nhập không hợp lệ hoặc đã bị đăng xuất",
                        success = false
                    });
                    return;
                }
                if (session.ExpiresAt.HasValue && session.ExpiresAt.Value < DateTime.UtcNow)
                {
                    db.JWTSessions.Remove(session);
                    await db.SaveChangesAsync();

                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        message = "Phiên đăng nhập đã hết hạn",
                        success = false
                    });
                    return;
                }
            }
            await _next(context);
        }
    }
}
