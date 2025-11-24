using System.Net;
using System.Text.Json;

namespace ProjectQLDCCT.Helpers.Middlewares
{
    public class CustomAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomAuthorizationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            await _next(context);

            // Sai quyền (forbidden)
            if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Bạn không có quyền sử dụng API này"
                });

                await context.Response.WriteAsync(json);
            }

            // Chưa login (Unauthorized)
            if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new
                {
                    success = false,
                    message = "Vui lòng đăng nhập để sử dụng API"
                });

                await context.Response.WriteAsync(json);
            }
        }
    }
}
