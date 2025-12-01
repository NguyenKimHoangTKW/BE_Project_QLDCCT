using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Authorize(Policy = "CTDT")]
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
        private async Task<string> GetUserPermissionNameCodeGV()
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

            var email = await db.Users
                .Where(x => x.id_users == userId)
                .Select(x => x.email)
                .FirstOrDefaultAsync();

            if (email == null)
                return "";

            var loadPermission = await db.CivilServants
                .Where(g => g.email == email)
                .Select(g => g.code_civilSer + " - " + g.fullname_civilSer)
                .FirstOrDefaultAsync();

            return loadPermission ?? "";
        }
        private int GetUserIdFromJWT()
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

        [HttpPost]
        [Route("loads-de-cuong-can-duyet")]
        public async Task<IActionResult> LoadDanhSachDeCuongCanDuyet([FromBody] SyllabusDTOs items)
        {
            var GetProgram = await GetUserPermissionPrograming();
            var listint = new int?[] { 2, 3, 4 ,7};
            var LoadSyllabus = db.Syllabi
                .Where(x => GetProgram.Contains(x.id_teacherbysubjectNavigation.id_courseNavigation.id_program ?? 0) && listint.Contains(x.id_status)).AsQueryable();
            var ListCount = new List<object>();
            var CountSyllabus_2 = await LoadSyllabus.Where(x => x.id_status == 2).CountAsync();
            var CountSyllabus_3 = await LoadSyllabus.Where(x => x.id_status == 3).CountAsync();
            var CountSyllabus_4 = await LoadSyllabus.Where(x => x.id_status == 4).CountAsync();
            var CountSyllabus_5 = await LoadSyllabus.Where(x => x.is_open_edit_final == 1).CountAsync();
            var CountSyllabus_6 = await LoadSyllabus.Where(x => x.id_status == 7).CountAsync();
            var CountSyllabus_7 = await LoadSyllabus.Where(x => x.is_open_edit_final == 2).CountAsync();
            ListCount.Add(new
            {
                dang_cho_duyet = CountSyllabus_2,
                tra_de_cuong = CountSyllabus_3,
                hoan_thanh = CountSyllabus_4,
                mo_de_cuong_sau_duyet = CountSyllabus_5,
                dang_mo_bo_sung_sau_duyet = CountSyllabus_6,
                tu_choi_mo_bo_sung = CountSyllabus_7
            });
            if (items.id_program > 0)
            {
                LoadSyllabus = LoadSyllabus.Where(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_program == items.id_program);
            }
            if (items.id_status > 0)
            {
                LoadSyllabus = LoadSyllabus.Where(x => x.id_status == items.id_status);
            }
            if (items.is_open_edit_final > 0)
            {
                LoadSyllabus = LoadSyllabus.Where(x => x.is_open_edit_final == items.is_open_edit_final);
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
                        x.is_open_edit_final
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
            var CheckSyllabus = await db.Syllabi
                .Include(x => x.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_semesterNavigation)
                .Include(x => x.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_key_year_semesterNavigation)
                .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);
            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            CheckSyllabus.returned_content = items.returned_content;
            CheckSyllabus.id_status = 3;
            var tbs = CheckSyllabus.id_teacherbysubjectNavigation;
            if (tbs == null)
                return Ok(new { success = false, message = "Không tìm thấy giảng viên phụ trách đề cương" });

            var course = tbs.id_courseNavigation;
            if (course == null)
                return Ok(new { success = false, message = "Không tìm thấy môn học" });
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = CheckSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa hoàn trả đề cương lại để chỉnh sửa lại",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);

            db.Notifications.Add(new Notification
            {
                id_user = tbs.id_user,
                id_program = null,
                title = "Hoàn trả đề cương không đạt chuẩn",
                message = $"Đề cương môn {course.code_course} - {course.name_course} - {course.id_semesterNavigation?.name_semester} - {course.id_key_year_semesterNavigation?.name_key_year_semester} của bạn đã bị hoàn trả để kiểm tra lại do không đạt chuẩn, vui lòng xem xét lại.",
                type = "refund_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Hoàn trả đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("approve-syllabus")]
        public async Task<IActionResult> BrowseSyllabus([FromBody] SyllabusDTOs items)
        {
            var GetID = GetUserIdFromJWT();

            var CheckSyllabus = await db.Syllabi
                .Include(x => x.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_semesterNavigation)
                .Include(x => x.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_key_year_semesterNavigation)
                .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);

            if (CheckSyllabus == null)
                return Ok(new { success = false, message = "Không tìm thấy đề cương" });

            var tbs = CheckSyllabus.id_teacherbysubjectNavigation;
            if (tbs == null)
                return Ok(new { success = false, message = "Không tìm thấy giảng viên phụ trách đề cương" });

            var course = tbs.id_courseNavigation;
            if (course == null)
                return Ok(new { success = false, message = "Không tìm thấy môn học" });

            CheckSyllabus.id_status = 4;
            CheckSyllabus.returned_content = null;

            var GetNameGV = await GetUserPermissionNameCodeGV();
            db.Log_Syllabi.Add(new Log_Syllabus
            {
                id_syllabus = CheckSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa hoàn tất duyệt đề cương",
                log_time = unixTimestamp
            });

            db.Notifications.Add(new Notification
            {
                id_user = tbs.id_user,
                id_program = null,
                title = "Duyệt đề cương thành công",
                message = $"Đề cương môn {course.code_course} - {course.name_course} - {course.id_semesterNavigation?.name_semester} - {course.id_key_year_semesterNavigation?.name_key_year_semester} của bạn đã được duyệt thành công",
                type = "approve_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "Duyệt đề cương thành công" });
        }


        [HttpPost] 
        [Route("log-hoat-dong-de-cuong")]
        public async Task<IActionResult> LoadLogSyllabus([FromBody] LogSyllabusDTOs items)
        {
            var LoadLogOperation = await db.Log_Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .OrderByDescending(x => x.id_log)
                .Select(x => new
                {
                    x.content_value,
                    x.log_time
                })
                .ToListAsync();
            return Ok(LoadLogOperation);
        }

        [HttpPost]
        [Route("preview-request-edit-syllabus")]
        public async Task<IActionResult> RequestEditSyllabus([FromBody] SyllabusDTOs items)
        {
            var CheckSyllabus = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => x.edit_content)
                .FirstOrDefaultAsync();
            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            return Ok(new { data = CheckSyllabus, success = true });
        }

        [HttpPost]
        [Route("accept-request-edit-syllabus")]
        public async Task<IActionResult> AcceptRequestEditSyllabus([FromBody] SyllabusDTOs items)
        {
            var CheckSyllabus = await db.Syllabi
                  .Include(x => x.id_teacherbysubjectNavigation)
                      .ThenInclude(t => t.id_courseNavigation)
                          .ThenInclude(c => c.id_semesterNavigation)
                  .Include(x => x.id_teacherbysubjectNavigation)
                      .ThenInclude(t => t.id_courseNavigation)
                          .ThenInclude(c => c.id_key_year_semesterNavigation)
                  .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);

            if (CheckSyllabus == null)
                return Ok(new { success = false, message = "Không tìm thấy đề cương" });

            var tbs = CheckSyllabus.id_teacherbysubjectNavigation;
            if (tbs == null)
                return Ok(new { success = false, message = "Không tìm thấy giảng viên phụ trách đề cương" });

            var course = tbs.id_courseNavigation;
            if (course == null)
                return Ok(new { success = false, message = "Không tìm thấy môn học" });
            CheckSyllabus.is_open_edit_final = 0;
            CheckSyllabus.edit_content = null;
            CheckSyllabus.id_status = 7;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = CheckSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa duyệt yêu cầu mở đề cương chỉnh sửa bổ sung sau duyệt",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
            db.Notifications.Add(new Notification
            {
                id_user = tbs.id_user,
                id_program = null,
                title = "Duyệt yêu cầu chỉnh sửa bổ sung đề cương sau duyệt",
                message = $"Đề cương môn {course.code_course} - {course.name_course} - {course.id_semesterNavigation?.name_semester} - {course.id_key_year_semesterNavigation?.name_key_year_semester} của bạn được mở chỉnh sửa bổ đề cương sau duyệt",
                type = "accept_request_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Duyệt thành công", success = true });
        }

        [HttpPost]
        [Route("cancer-request-edit-syllabus")]
        public async Task<IActionResult> CancerRequestEditSyllabus([FromBody] SyllabusDTOs items)
        {
            if (string.IsNullOrEmpty(items.returned_content))
            {
                return Ok(new { message = "Không được để trống lý do từ chối yêu cầu mở chỉnh sửa", success = false });
            }
            var CheckSyllabus = await db.Syllabi
                   .Include(x => x.id_teacherbysubjectNavigation)
                       .ThenInclude(t => t.id_courseNavigation)
                           .ThenInclude(c => c.id_semesterNavigation)
                   .Include(x => x.id_teacherbysubjectNavigation)
                       .ThenInclude(t => t.id_courseNavigation)
                           .ThenInclude(c => c.id_key_year_semesterNavigation)
                   .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);

            if (CheckSyllabus == null)
                return Ok(new { success = false, message = "Không tìm thấy đề cương" });

            var tbs = CheckSyllabus.id_teacherbysubjectNavigation;
            if (tbs == null)
                return Ok(new { success = false, message = "Không tìm thấy giảng viên phụ trách đề cương" });

            var course = tbs.id_courseNavigation;
            if (course == null)
                return Ok(new { success = false, message = "Không tìm thấy môn học" });

            CheckSyllabus.is_open_edit_final = 2;
            CheckSyllabus.returned_content = items.returned_content;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = CheckSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa từ chối yêu cầu mở đề cương bổ sung sau duyệt",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
            db.Notifications.Add(new Notification
            {
                id_user = tbs.id_user,
                id_program = null,
                title = "Từ chối yêu cầu chỉnh sửa bổ sung đề cương sau duyệt",
                message = $"Đề cương môn {course.code_course} " +
                $"- {course.name_course} " +
                $"- {course.id_semesterNavigation?.name_semester} " +
                $"- {course.id_key_year_semesterNavigation?.name_key_year_semester} của bạn bị từ chối mở bổ sung đề cương sau duyệt",
                type = "cancer_request_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Từ chối yêu cầu thành công", success = true });
        }
    }
}
