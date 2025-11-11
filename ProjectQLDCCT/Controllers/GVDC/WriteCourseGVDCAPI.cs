using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Route("api/dvdc/write-course")]
    [ApiController]
    public class WriteCourseGVDCAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public WriteCourseGVDCAPI(QLDCContext _db)
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

        [HttpGet]
        [Route("loads-danh-sach-de-cuong-can-soan")]
        public async Task<IActionResult> LoadCourseByPermission()
        {
            var List = await GetUserPermissionCourse();
            var ListCourse = await db.TeacherBySubjects
                .Where(x => List.Contains(x.id_course ?? 0))
                .Select(x => new
                {
                    x.id_courseNavigation.code_course,
                    x.id_courseNavigation.name_course,
                    x.id_courseNavigation.id_gr_courseNavigation.name_gr_course,
                    x.id_courseNavigation.credits,
                    x.id_courseNavigation.totalTheory,
                    x.id_courseNavigation.totalPractice,
                    x.id_courseNavigation.id_isCourseNavigation.name,
                    x.id_courseNavigation.id_key_year_semesterNavigation.name_key_year_semester,
                    x.id_courseNavigation.id_semesterNavigation.name_semester,
                    x.id_courseNavigation.id_course,
                })
                .ToListAsync();
            if (ListCourse.Count > 0)
            {
                return Ok(new { data = ListCourse, success = true });
            }
            else
            {
                return Ok(new { message = "Bạn chưa có học phần được phân để viết đề cương.", success = false });
            }
        }
    }
}
