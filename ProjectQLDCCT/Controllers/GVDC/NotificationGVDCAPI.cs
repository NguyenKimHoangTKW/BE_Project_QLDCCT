using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Route("api/gvdc/notification")]
    [ApiController]
    public class NotificationGVDCAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public NotificationGVDCAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private async Task<List<int>> GetUserPermissionCourse()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Thiếu cookie JWT hoặc chưa đăng nhập.");

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc bị sửa đổi.");
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("Token không chứa id_users.");

            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Giá trị id_users trong token không hợp lệ.");

            var loadPermission = await db.TeacherBySubjects
                .Where(x => x.id_user == userId)
                .Select(x => x.id_course ?? 0)
                .ToListAsync();
            return loadPermission;
        }
        private async Task<int> GetUserPermissionIDUser()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(token))
                throw new UnauthorizedAccessException("Thiếu cookie JWT hoặc chưa đăng nhập.");

            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;

            try
            {
                jwtToken = handler.ReadJwtToken(token);
            }
            catch
            {
                throw new UnauthorizedAccessException("Token không hợp lệ hoặc bị sửa đổi.");
            }

            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Token không chứa id_users hợp lệ.");

            return userId;
        }
        [HttpPost("view-all-notification")]
        public async Task<IActionResult> ViewAllNotification([FromBody] NotificationDTOs items)
        {
            var userId = await GetUserPermissionIDUser();
            var totalRecords = await db.Notifications
                .Where(x => x.id_user == userId && x.id_program == null).CountAsync();
            var GetList = await db.Notifications
                .Where(x => x.id_user == userId && x.id_program == null)
                .OrderByDescending(x => x.id_notification)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_notification,  
                    x.title,
                    x.message,
                    x.type,
                    x.create_time,
                    x.is_read,
                    x.link
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                GetList,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });

        }
        [HttpGet("count")]
        public async Task<IActionResult> CountNotification()
        {
            var userId = await GetUserPermissionIDUser();

            var count = await db.Notifications
                .Where(x => x.id_user == userId && x.is_read == false)
                .CountAsync();

            return Ok(new
            {
                success = true,
                count
            });
        }
        [HttpPost("update-is-read-true")]
        public async Task<IActionResult> UpdateIsReadTrue([FromBody] NotificationDTOs items)
        {
            var Getitems = await db.Notifications.FirstOrDefaultAsync(x => x.id_notification == items.id_notification);
            Getitems.is_read = true;
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost("update-is-read-false")]
        public async Task<IActionResult> UpdateIsReadFalse([FromBody] NotificationDTOs items)
        {
            var Getitems = await db.Notifications.FirstOrDefaultAsync(x => x.id_notification == items.id_notification);
            Getitems.is_read = false;
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost("delete")]
        public async Task<IActionResult> DeleteNotification([FromBody] NotificationDTOs items)
        {
            var Getitems = await db.Notifications.FirstOrDefaultAsync(x => x.id_notification == items.id_notification);
            db.Notifications.Remove(Getitems);
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost("update-all-is-read-false")]
        public async Task<IActionResult> UpdateAllIsReadFalse()
        {
            var userId = await GetUserPermissionIDUser();

            await db.Notifications
                .Where(x => x.id_user == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.is_read, true));

            return Ok(new { success = true });
        }

        [HttpPost("update-all-is-read-true")]
        public async Task<IActionResult> UpdateAllIsReadTrue()
        {
            var userId = await GetUserPermissionIDUser();

            await db.Notifications
                .Where(x => x.id_user == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.is_read, false));

            return Ok(new { success = true });
        }
        [HttpPost("delete-all")]
        public async Task<IActionResult> DeleteAllNotifications()
        {
            var userId = await GetUserPermissionIDUser();

            await db.Notifications
                .Where(x => x.id_user == userId)
                .ExecuteDeleteAsync();

            return Ok(new { success = true });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetListNotification()
        {
            var userId = await GetUserPermissionIDUser();

            var list = await db.Notifications
                .Where(x => x.id_user == userId && x.id_program == null)
                .OrderByDescending(x => x.create_time)
                .Take(5)
                .Select(x => new
                {
                    x.id_notification,
                    x.title,
                    x.message,
                    x.link,
                    x.create_time,
                    x.is_read
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = list
            });
        }

    }
}
