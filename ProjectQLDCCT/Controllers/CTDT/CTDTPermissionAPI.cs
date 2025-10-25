using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/ctdt/permission")]
    [ApiController]
    public class CTDTPermissionAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CTDTPermissionAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpGet]
        [Route("loads-ctdt-by-permission")]
        public async Task<IActionResult> LoadListCTDTByPermission()
        {
            var token = HttpContext.Request.Cookies["jwt"];

            if (string.IsNullOrWhiteSpace(token))
                return Unauthorized(new { success = false, message = "Thiếu cookie JWT hoặc chưa đăng nhập." });

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                return Unauthorized(new { success = false, message = "Token không hợp lệ hoặc bị sửa đổi." });
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { success = false, message = "Token không chứa id_users." });

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Giá trị id_users trong token không hợp lệ." });

            var loadPermission = await db.UserByFaculPrograms
                .Where(x => x.id_users == userId && x.id_program != null)
                .Select(x => new
                {
                    value = x.id_programNavigation.id_program,
                    text = x.id_programNavigation.name_program,
                })
                .ToListAsync();
            return Ok(loadPermission);
        }
    }
}
