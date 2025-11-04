using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.Shared
{
    [Route("api/evaluate")]
    [ApiController]
    public class PreviewEvaluateAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public PreviewEvaluateAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        private async Task<List<int>> GetUserPermissionFaculties()
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

            var loadPermission = await db.UserByFaculPrograms
                .Where(x => x.id_users == userId && x.id_faculty != null)
                .Select(x => x.id_facultyNavigation.id_faculty)
                .ToListAsync();

            return loadPermission;
        }
        private async Task<List<int>> GetUserPermissionProgramingbyFaculty()
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

            var loadPermission = await db.UserByFaculPrograms
                .Where(x => x.id_users == userId && x.id_program != null)
                .Select(x => (int)x.id_programNavigation.id_faculty)
                .ToListAsync();
            return loadPermission;
        }
        private async Task<List<int>> GetUserPermissionPrograming()
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

            var loadPermission = await db.UserByFaculPrograms
                .Where(x => x.id_users == userId && x.id_program != null)
                .Select(x => (int)x.id_programNavigation.id_program)
                .ToListAsync();
            return loadPermission;
        }
        [HttpGet]
        [Route("preview-course-objectives")]
        public async Task<IActionResult> LoadsPreviewCO()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var GetProgram = await GetUserPermissionProgramingbyFaculty();

            var loadsdata = await db.CourseObjectives
                .Where(x => GetFaculty.Count > 0
                    ? GetFaculty.Contains(x.id_faculty ?? 0)
                    : GetProgram.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity
                })
                .ToListAsync();

            return Ok(new { success = true, data = loadsdata });
        }

        [HttpGet]
        [Route("preview-course-learning-outcomes")]
        public async Task<IActionResult> LoadsPreviewCLO()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var GetProgram = await GetUserPermissionProgramingbyFaculty();

            var loadsdata = await db.CourseLearningOutcomes
                .Where(x => GetFaculty.Count > 0
                    ? GetFaculty.Contains(x.id_faculty ?? 0)
                    : GetProgram.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level
                })
                .ToListAsync();
            return Ok(new { success = true, data = loadsdata });
        }
        [HttpPost]
        [Route("preview-program-learning-outcome")]
        public async Task<IActionResult> LoadsPreviewPLO([FromBody] PLODTOs items)
        {
            var GetProgram = await GetUserPermissionPrograming();
            var listData = new List<object>();
            var LoadPLO = await db.ProgramLearningOutcomes
                .OrderBy(x => x.order_index)
                .Where(x => GetProgram.Count > 0 ? GetProgram.Contains(x.Id_Program ?? 0) : x.Id_Program == items.Id_Program)
                .ToListAsync();
            foreach (var item in LoadPLO)
            {
                var GetListPI = await db.PerformanceIndicators
                    .Where(x => x.Id_PLO == item.Id_Plo)
                    .OrderBy(x => x.order_index)
                    .Select(x => new
                    {
                        x.code,
                        x.Description,
                    }).ToListAsync();
                listData.Add(new
                {
                    code_plo = item.code,
                    description_plo = item.Description,
                    count_pi = GetListPI.Count,
                    pi = GetListPI
                });
            }
            return Ok(listData);
        }
    }
}
