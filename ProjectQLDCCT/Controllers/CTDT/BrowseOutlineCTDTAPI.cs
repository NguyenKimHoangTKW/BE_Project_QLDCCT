using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/ctdt/browse-outline")]
    [ApiController]
    public class BrowseOutlineCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public BrowseOutlineCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
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

        [HttpPost]
        [Route("loads-de-cuong-can-duyet")]
        public async Task<IActionResult> LoadDanhSachDeCuongCanDuyet([FromBody] SyllabusDTOs items)
        {
            var GetProgram = await GetUserPermissionPrograming();
            var listint = new int?[] { 2, 3, 4 };
            var LoadSyllabus = db.Syllabi
                .Where(x => GetProgram.Contains(x.id_teacherbysubjectNavigation.id_courseNavigation.id_program ?? 0) && listint.Contains(x.id_status ?? 0)).AsQueryable();
            var ListCount = new List<object>();
            var CountSyllabus_2 = await LoadSyllabus.Where(x => x.id_status == 2).CountAsync();
            var CountSyllabus_3 = await LoadSyllabus.Where(x => x.id_status == 3).CountAsync();
            var CountSyllabus_4 = await LoadSyllabus.Where(x => x.id_status == 4).CountAsync();

            ListCount.Add(new
            {
                dang_cho_duyet = CountSyllabus_2,
                tra_de_cuong = CountSyllabus_3,
                hoan_thanh = CountSyllabus_4
            });
            if (items.id_program > 0)
            {
                LoadSyllabus = LoadSyllabus.Where(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_program == items.id_program);
            }
            if (items.id_status > 0)
            {
                LoadSyllabus = LoadSyllabus.Where(x => x.id_status == items.id_status);
            }

            var query = await LoadSyllabus
                    .Select(x => new
                    {
                        x.id_syllabus,
                        x.version,
                        x.id_status,
                        x.time_cre,
                        x.time_up,
                        code_course = x.id_teacherbysubjectNavigation.id_courseNavigation.code_course,
                        name_course = x.id_teacherbysubjectNavigation.id_courseNavigation.name_course,
                        semester = x.id_teacherbysubjectNavigation.id_courseNavigation.id_semesterNavigation.name_semester,
                        key_year = x.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semesterNavigation.name_key_year_semester,
                        program = x.id_teacherbysubjectNavigation.id_courseNavigation.id_programNavigation.name_program,
                        code_civil = db.CivilServants.Where(g => g.email == x.id_teacherbysubjectNavigation.id_userNavigation.email).Select(g => g.code_civilSer).FirstOrDefault(),
                        name_civil = db.CivilServants.Where(g => g.email == x.id_teacherbysubjectNavigation.id_userNavigation.email).Select(g => g.fullname_civilSer).FirstOrDefault(),
                        email_civil = db.CivilServants.Where(g => g.email == x.id_teacherbysubjectNavigation.id_userNavigation.email).Select(g => g.email).FirstOrDefault(),
                    })
                    .ToListAsync();
            if (query.Count > 0)
            {
                return Ok(new { message = "Tải dữ liệu thành công", data = query, success = true, count = ListCount });
            }
            else
            {
                return Ok(new { message = "Chưa có đề cương môn học nào cần duyệt", success = false, count = ListCount });
            }
        }
        [HttpPost]
        [Route("refund-syllabus")]
        public async Task<IActionResult> RefundSyllabus([FromBody] SyllabusDTOs items)
        {
            if (string.IsNullOrEmpty(items.returned_content))
                return Ok(new { message = "Vui lòng nhập lý do hoàn trả đề cương để chỉnh sửa", success = false });
            var CheckSyllabus = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus).FirstOrDefaultAsync();
            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            CheckSyllabus.returned_content = items.returned_content;
            CheckSyllabus.id_status = 3;
            await db.SaveChangesAsync();
            return Ok(new { message = "Hoàn trả đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("approve-syllabus")]
        public async Task<IActionResult> BrowseSyllabus([FromBody] SyllabusDTOs items)
        {
            var CheckSyllabus = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus).FirstOrDefaultAsync();
            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            CheckSyllabus.id_status = 4;
            CheckSyllabus.returned_content = null;
            await db.SaveChangesAsync();
            return Ok(new { message = "Hoàn trả đề cương thành công", success = true });
        }
    }
}
