using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/course-objectives")]
    [ApiController]
    public class COsDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();

        public COsDVAPI(QLDCContext _db)
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
        [Route("load-du-lieu-muc-tieu-hoc-phan")]
        public async Task<IActionResult> LoadData([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var totalRecords = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .CountAsync();
            var GetItems = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id,
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity,
                    x.time_up,
                    x.time_cre
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
        [Route("them-moi-muc-tieu-hoc-phan")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();

            if (string.IsNullOrEmpty(items.name_CO))
                return Ok(new { message = "Không được bỏ trống trường Tên Mục tiêu học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung Mục tiêu học phần", success = false });
            var CheckCO = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0) && x.name_CO.ToLower().Trim() == items.name_CO.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckCO != null)
                return Ok(new { message = "Mục tiêu học phần này đã tồn tại, vui lòng kiểm tra lại dữ liệu", success = false });

            var new_record = new CourseObjective
            {
                name_CO = items.name_CO,
                describe_CO = items.describe_CO,
                typeOfCapacity = items.typeOfCapacity,
                id_faculty = GetFaculty.FirstOrDefault(),
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CourseObjectives.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-muc-tieu-hoc-phan")]
        public async Task<IActionResult> InfoCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var checkInfo = await db.CourseObjectives
                .Where(x => x.id == items.id)
                .Select(x => new
                {
                    x.id,
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity,
                })
                .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Mục tiêu học phần", success = false });
            return Ok(checkInfo);
        }
        [HttpPost]
        [Route("update-muc-tieu-hoc-phan")]
        public async Task<IActionResult> UpdateCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();

            if (string.IsNullOrEmpty(items.name_CO))
                return Ok(new { message = "Không được bỏ trống trường Tên Mục tiêu học phần", success = false });

            if (string.IsNullOrEmpty(items.describe_CO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung Mục tiêu học phần", success = false });

            var checkCO = await db.CourseObjectives
                .FirstOrDefaultAsync(x => x.id == items.id);

            if (checkCO == null)
                return Ok(new { message = "Không tìm thấy thông tin Mục tiêu học phần", success = false });

            checkCO.name_CO = items.name_CO.Trim();
            checkCO.describe_CO = items.describe_CO.Trim();
            checkCO.typeOfCapacity = items.typeOfCapacity?.Trim();
            checkCO.time_up = unixTimestamp;

            await db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin Mục tiêu học phần thành công", success = true });
        }

        [HttpPost]
        [Route("xoa-du-lieu-muc-tieu-hoc-phan")]
        public async Task<IActionResult> DeleteCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var CheckCLO_CO = await db.CLO_CO_Mappings.FirstOrDefaultAsync(x => x.id_CO == items.id);
            if (CheckCLO_CO != null)
                return Ok(new { message = "Mục tiêu học phần này đang tồn tại trong đề cương, không thể xóa", success = false });
            var CheckCO = await db.CourseObjectives.FirstOrDefaultAsync(x => x.id == items.id);
            if (CheckCO == null)
                return Ok(new { message = "Không tìm thầy thông tin Mục tiêu học phần", success = false });
            db.CourseObjectives.Remove(CheckCO);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
