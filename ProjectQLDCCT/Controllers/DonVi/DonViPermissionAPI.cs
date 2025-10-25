using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/permission")]
    [ApiController]
    public class DonViPermissionAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public DonViPermissionAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpGet]
        [Route("loads-donvi-by-permission")]
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
                .Where(x => x.id_users == userId && x.id_faculty != null)
                .Select(x => new
                {
                    value = x.id_facultyNavigation.id_faculty,
                    text = x.id_facultyNavigation.name_faculty,
                })
                .ToListAsync();
            return Ok(loadPermission);
        }
        [HttpGet]
        [Route("loads-ctdt-by-permission-donvi")]
        public async Task<IActionResult> LoadsListCTDTByPermissionDonVi()
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
                 .Where(x => x.id_users == userId && x.id_faculty != null)
                 .Select(x => x.id_facultyNavigation.id_faculty)
                 .ToListAsync();

            var loadCTDT = await db.TrainingPrograms
                .Where(x => loadPermission.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    value = x.id_program,
                    text = x.name_program
                })
                .ToListAsync();
            return Ok(loadCTDT);
        }
    }
}
