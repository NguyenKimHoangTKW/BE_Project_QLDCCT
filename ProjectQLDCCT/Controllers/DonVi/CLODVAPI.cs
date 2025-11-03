using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/course-learning-outcome")]
    [ApiController]
    public class CLODVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public CLODVAPI(QLDCContext _db)
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
        [HttpPost]
        [Route("load-du-lieu-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> LoadCLO([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var totalRecords = await db.CourseLearningOutcomes.Where(x => GetFaculty.Contains(x.id_faculty ?? 0)).CountAsync();
            var GetItems = await db.CourseLearningOutcomes
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(totalRecords)
                .Select(x => new
                {
                    x.id,
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level,
                    x.time_cre,
                    x.time_up
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = GetItems,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> AddNew([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.name_CLO))
                return Ok(new { message = "Không được bỏ trống trường Tên chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CLO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.bloom_level))
                return Ok(new { message = "Không được bỏ trống trường Mức độ Bloom chuẩn đầu ra học phần", success = false });
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.name_CLO.ToLower().Trim() == items.name_CLO.ToLower().Trim()).FirstOrDefaultAsync();
            if (checkCLO != null)
                return Ok(new { message = "Chuẩn đầu ra học phần này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var new_record = new CourseLearningOutcome
            {
                name_CLO = items.name_CLO,
                describe_CLO = items.describe_CLO,
                id_faculty = GetFaculty.FirstOrDefault(),
                bloom_level = items.bloom_level,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CourseLearningOutcomes.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> InfoCLO([FromBody] CLODTOs items)
        {
            var CheckCLO = await db.CourseLearningOutcomes
                .Where(x => x.id == items.id)
                .Select(x => new
                {
                    x.id,
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level,
                    x.program_id
                })
                .FirstOrDefaultAsync();
            if (CheckCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            return Ok(CheckCLO);
        }
        [HttpPost]
        [Route("cap-nhat-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> UpdateCLO([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.name_CLO))
                return Ok(new { message = "Không được bỏ trống trường Tên chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CLO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.bloom_level))
                return Ok(new { message = "Không được bỏ trống trường Mức độ Bloom chuẩn đầu ra học phần", success = false });
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.id == items.id).FirstOrDefaultAsync();
            if (checkCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            checkCLO.name_CLO = items.name_CLO;
            checkCLO.describe_CLO = items.describe_CLO;
            checkCLO.bloom_level = items.bloom_level;
            checkCLO.program_id = items.program_id;
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> DeleteCLO([FromBody] CLODTOs items)
        {
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.id == items.id).FirstOrDefaultAsync();
            if (checkCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            db.CourseLearningOutcomes.Remove(checkCLO);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
