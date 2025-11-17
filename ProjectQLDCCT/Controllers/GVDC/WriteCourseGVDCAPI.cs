using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.Xml;

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Route("api/gvdc/write-course")]
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
                     x.id_teacherbysubject,
                     x.id_course,
                     x.id_courseNavigation.code_course,
                     x.id_courseNavigation.name_course,
                     name_gr_course = x.id_courseNavigation.id_gr_courseNavigation.name_gr_course,
                     x.id_courseNavigation.credits,
                     x.id_courseNavigation.totalTheory,
                     x.id_courseNavigation.totalPractice,
                     x.id_courseNavigation.id_isCourseNavigation.name,
                     name_isCourse = x.id_courseNavigation.id_isCourseNavigation.name,
                     name_key_year_semester = x.id_courseNavigation.id_key_year_semesterNavigation.name_key_year_semester,
                     name_semester = x.id_courseNavigation.id_semesterNavigation.name_semester,
                     name_program = x.id_courseNavigation.id_programNavigation.name_program,
                     time_open = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == x.id_course)
                         .Select(g => g.open_time)
                         .FirstOrDefault(),
                     time_close = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == x.id_course)
                         .Select(g => g.close_time)
                         .FirstOrDefault(),
                     is_open = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == x.id_course)
                         .Select(g => g.is_open)
                         .FirstOrDefault()
                 })
                 .ToListAsync();
            if (ListCourse.Count > 0)
            {
                return Ok(new { data = ListCourse, message = "Tải dữ liệu thành công", success = true });
            }
            else
            {
                return Ok(new { message = "Bạn chưa có học phần được phân để viết đề cương.", success = false });
            }
        }

        [HttpPost]
        [Route("danh-sach-giang-vien-viet-de-cuong-trong-mon-hoc")]
        public async Task<IActionResult> ListCourseByID([FromBody] SyllabusDTOs items)
        {
            var listData = await db.Syllabi
                .Where(x => x.id_teacherbysubjectNavigation.id_course == items.id_course)
                .Select(x => new
                {
                    x.id_syllabus,
                    value = x.id_teacherbysubject,
                    code_status = x.id_status,
                    status = x.id_statusNavigation.name,
                    version = x.version,
                    is_open = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == items.id_course)
                         .Select(g => g.is_open)
                         .FirstOrDefault(),
                    time_open = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == items.id_course)
                         .Select(g => g.open_time)
                         .FirstOrDefault(),
                    time_close = db.OpenSyllabusWindowsCourses
                         .Where(g => g.id_course == items.id_course)
                         .Select(g => g.close_time)
                         .FirstOrDefault(),
                    civilServants = db.CivilServants
                        .Where(cs => cs.email == x.create_byNavigation.email)
                        .Select(cs => new
                        {
                            cs.code_civilSer,
                            fullname_civilSer = cs.fullname_civilSer,
                            name_program = cs.id_programNavigation.name_program,
                            cs.email
                        })
                        .ToList()
                })
                .ToListAsync();
            var ListData = new List<object>();
            var window = db.OpenSyllabusWindowsCourses
                 .Where(g => g.id_course == items.id_course)
                 .Select(g => new
                 {
                     time_open = g.open_time,
                     time_close = g.close_time
                 })
                 .FirstOrDefault();
            var GetNameCourse = await db.Courses
                .Where(x => x.id_course == items.id_course)
                .Select(x => x.name_course)
                .FirstOrDefaultAsync();
            if (listData.Any())
                return Ok(new { data = listData, name_course = GetNameCourse, success = true });
            else

                return Ok(new { name_course = GetNameCourse, data = window, message = "Chưa có dữ liệu giảng viên viết đề cương", success = false });
        }

        [HttpPost]
        [Route("tao-moi-mau-de-cuong-cho-mon-hoc")]
        public async Task<IActionResult> TaoMoiMauDeCuong([FromBody] SyllabusDTOs items)
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
            var CheckexistingFinalVersions = await db.Syllabi
              .Where(x => x.id_teacherbysubject == items.id_teacherbysubject && x.create_by == userId && x.id_status == 4)
              .ToListAsync();
            if (CheckexistingFinalVersions.Any())
            {
                return Ok(new { message = "Bạn đó có đề cương đã hoàn thiện trong môn học này, không thể tạo thêm", success = false });
            }
            var existingVersions = await db.Syllabi
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject && x.create_by == userId)
                .Select(x => x.version)
                .ToListAsync();
            var GetJsonTempalte = await db.TeacherBySubjects
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject)
                .Select(x => db.SyllabusTemplates.Where(g => g.id_faculty == x.id_courseNavigation.id_programNavigation.id_faculty).Select(g => g.template_json).FirstOrDefault()).FirstOrDefaultAsync();
            int nextVersion = 1;
            var numericVersions = existingVersions
                .Select(v => int.TryParse(v, out int n) ? n : 0)
                .ToList();

            if (numericVersions.Any())
                nextVersion = numericVersions.Max() + 1;

            var new_record = new Syllabus
            {
                id_teacherbysubject = items.id_teacherbysubject,
                id_status = 1,
                version = nextVersion.ToString(),
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                create_by = userId,
                syllabus_json = GetJsonTempalte
            };

            db.Syllabi.Add(new_record);
            await db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Tạo mẫu đề cương phiên bản {nextVersion} thành công.",
                success = true,
                version = nextVersion
            });
        }

        [HttpPost]
        [Route("inherit-template-syllabus")]
        public async Task<IActionResult> inheritTempalte([FromBody] InheritTemplateSyllabusTemplateDTOs items)
        {
            var listData = await db.Syllabi
                .Where(x => x.id_teacherbysubjectNavigation.id_course == items.id_course && x.id_status == 4)
                .ToListAsync();
            if (listData.Count <= 1)
            {
                return Ok(new { message = "Không có đề cương hoàn thiện để kế thừa, không thể sử dụng chức năng này", success = false });
            }
            var CheckTemplate_1 = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus1).FirstOrDefaultAsync();

            var checkTemplate_2 = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus2).Select(x => x.syllabus_json).FirstOrDefaultAsync();

            CheckTemplate_1.syllabus_json = checkTemplate_2;
            CheckTemplate_1.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Kế thừa mẫu đề cương thành công", success = true });
        }
    }
}
