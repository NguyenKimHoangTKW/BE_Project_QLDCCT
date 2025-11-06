using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/level-contribution")]
    [ApiController]
    public class LevelContributionDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public LevelContributionDVAPI(QLDCContext _db)
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
        [HttpGet]
        [Route("loads-muc-do-dong-gop")]
        public async Task<IActionResult> LoadDuLieuLC()
        {
            GetFaculty = await GetUserPermissionFaculties();
            var LoadData = await db.LevelContributions
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id,
                    x.Code,
                    x.Description,
                    x.time_cre,
                    x.time_up,
                })
                .ToListAsync();
            return Ok(new { data = LoadData, success = true });
        }
        [HttpPost]
        [Route("them-moi-muc-do-dong-gop")]
        public async Task<IActionResult> CreateLevelContribution([FromBody] LevelContributionDTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.Code))
                return Ok(new { message = "Không được bỏ trống trường Mã mức độ đóng góp", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống trường Nội dung mức độ đóng góp", success = false });
            var CheckLC = await db.LevelContributions.Where(x => x.Code.ToLower().Trim() == items.Code.ToLower().Trim()).FirstOrDefaultAsync();
            if (CheckLC != null)
                return Ok(new { message = "Mức độ đóng góp này đã tồn tại, vui lòng kiểm tra lại", success = false });
            var new_record = new LevelContribution
            {
                Code = items.Code,
                Description = items.Description,
                id_faculty = GetFaculty.FirstOrDefault(),
                time_cre = unixTimestamp,
                time_up = unixTimestamp
            };
            db.LevelContributions.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-muc-do-dong-gop")]
        public async Task<IActionResult> InfoLevelContribution([FromBody] LevelContributionDTOs items)
        {
            var CheckLC = await db.LevelContributions
                .Where(x => x.id == items.id)
                .Select(x => new
                {
                    x.id,
                    x.Code,
                    x.Description
                })
                .FirstOrDefaultAsync();
            if (CheckLC == null)
                return Ok(new { message = "Không tìm thấy thông tin Mức độ đóng góp", success = false });
            return Ok(new { data = CheckLC, success = true });
        }
        [HttpPost]
        [Route("update-muc-do-dong-gop")]
        public async Task<IActionResult> UpdateLevelContribution([FromBody] LevelContributionDTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.Code))
                return Ok(new { message = "Không được bỏ trống trường Mã mức độ đóng góp", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống trường Nội dung mức độ đóng góp", success = false });
            var CheckLC = await db.LevelContributions
                .Where(x => x.id == items.id)
                .FirstOrDefaultAsync();
            if (CheckLC == null)
                return Ok(new { message = "Mức độ đóng góp này đã tồn tại, vui lòng kiểm tra lại", success = false });
            CheckLC.Code = items.Code;
            CheckLC.Description = items.Description;
            CheckLC.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("delete-muc-do-dong-gop")]
        public async Task<IActionResult> DeleteLevelContribution([FromBody] LevelContributionDTOs items)
        {
            var CheckLC = await db.LevelContributions
              .Where(x => x.id == items.id)
              .FirstOrDefaultAsync();
            if (CheckLC == null)
                return Ok(new { message = "Mức độ đóng góp này đã tồn tại, vui lòng kiểm tra lại", success = false });
            db.LevelContributions.Remove(CheckLC);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
