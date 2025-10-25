using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/syllabustemplate")]
    [ApiController]
    public class TemplateSyllabusDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public TemplateSyllabusDVAPI(QLDCContext _db)
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
        [Route("load-mau-de-cuong")]
        public async Task<IActionResult> TemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var loadPermision = await GetUserPermissionFaculties();
            var totalRecords = await db.SyllabusTemplates.Where(x => loadPermision.Contains(x.id_faculty ?? 0)).CountAsync();
            var GetItems = await db.SyllabusTemplates
                .Where(x => loadPermision.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id_template)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_template,
                    x.template_name,
                    is_default = x.is_default == 1 ? "Mở mẫu đề cương" : "Đóng mẫu đề cương",
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
        [Route("them-moi-mau-de-cuong")]
        public async Task<IActionResult> ThemMoiTempalteSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var loadPermision = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.template_name))
                return Ok(new { message = "Không được bỏ trống trường Tên mẫu đề cương", success = false });
            var CheckSyllabusTemplate = await db.SyllabusTemplates.FirstOrDefaultAsync(x => x.template_name.ToLower().Trim() == items.template_name.ToLower().Trim() && loadPermision.Contains(x.id_faculty ?? 0));
            if (CheckSyllabusTemplate != null)
                return Ok(new { message = "Tên mẫu đề cương này đã tồn tại, vui lòng kiểm tra lại.", success = false });

            var new_record = new SyllabusTemplate
            {
                template_name = items.template_name,
                is_default = items.is_default,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                id_faculty = loadPermision.FirstOrDefault()
            };
            db.SyllabusTemplates.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-mau-de-cuong")]
        public async Task<IActionResult> InfoTemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var CheckInfo = await db.SyllabusTemplates
                .Where(x => x.id_template == items.id_template)
                .Select(x => new
                {
                    x.id_template,
                    x.template_name,
                    x.is_default,
                })
                .FirstOrDefaultAsync();
            if (CheckInfo == null)
                return Ok(new { message = "Không tìm thấy thông tin mẫu đề cương", success = false });
            return Ok(new { data = CheckInfo, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-mau-de-cuong")]
        public async Task<IActionResult> UpdateTempalteSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            if (string.IsNullOrEmpty(items.template_name))
                return Ok(new { message = "Không được bỏ trống trường Tên mẫu đề cương", success = false });

            var checkTemplate = await db.SyllabusTemplates.FirstOrDefaultAsync(x => x.id_template == items.id_template);
            if (checkTemplate == null)
                return Ok(new { message = "Không tìm thấy thông tin Mẫu đề cương", success = false });

            checkTemplate.is_default = items.is_default;
            checkTemplate.template_name = items.template_name;
            checkTemplate.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-mau-de-cuong")]
        public async Task<IActionResult> DeleteTemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var CheckSection = await db.SyllabusTemplateSections.Where(x => x.id_template == items.id_template).Select(x => x.id_template).ToListAsync();
            var checkSyllabusSection = await db.SyllabusTemplateSections.Where(x => CheckSection.Contains(x.id_template)).ToListAsync();
            return Ok();
        }
    }
}
