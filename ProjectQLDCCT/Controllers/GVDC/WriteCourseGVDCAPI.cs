using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers.SignalR;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Dynamic.Core;
using System.Security.Cryptography.Xml;

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Authorize(Policy = "GVDC")]
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

        [HttpPost]
        [Route("loads-danh-sach-de-cuong-can-soan")]
        public async Task<IActionResult> LoadCourseByPermission([FromBody] SyllabusDTOs items)
        {
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var listWindow = await db.OpenSyllabusWindowsCourses.ToListAsync();

            foreach (var w in listWindow)
            {
                if (w.open_time == null || w.close_time == null ||
                    unixTimestamp < w.open_time || unixTimestamp > w.close_time)
                {
                    w.is_open = 0;
                }
                else
                {
                    w.is_open = 1;
                }
            }
            await db.SaveChangesAsync();

            var userCourses = await GetUserPermissionCourse();

            var query = db.TeacherBySubjects
                .Where(x => userCourses.Contains(x.id_course ?? 0))
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
                    name_isCourse = x.id_courseNavigation.id_isCourseNavigation.name,
                    name_key_year_semester = x.id_courseNavigation.id_key_year_semesterNavigation.name_key_year_semester,
                    name_semester = x.id_courseNavigation.id_semesterNavigation.name_semester,
                    name_program = x.id_courseNavigation.id_programNavigation.name_program,
                    window = db.OpenSyllabusWindowsCourses
                        .Where(g => g.id_course == x.id_course)
                        .Select(g => new { g.open_time, g.close_time, g.is_open })
                        .FirstOrDefault()
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(items.searchTerm))
            {
                string keyword = items.searchTerm.Trim().ToLower();

                query = query.Where(x =>
                    (x.code_course ?? "").ToLower().Contains(keyword) ||
                    (x.name_course ?? "").ToLower().Contains(keyword) ||
                    (x.name_gr_course ?? "").ToLower().Contains(keyword) ||
                    (x.name_isCourse ?? "").ToLower().Contains(keyword) ||
                    (x.name_key_year_semester ?? "").ToLower().Contains(keyword) ||
                    (x.name_semester ?? "").ToLower().Contains(keyword) ||
                    (x.name_program ?? "").ToLower().Contains(keyword)
                );
            }

            var raw = await query
                .OrderByDescending(x => x.id_teacherbysubject)
                .ToListAsync();

            var ListCourse = raw
                .GroupBy(x => x.id_course)
                .Select(g => g.First())
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_teacherbysubject,
                    x.id_course,
                    x.code_course,
                    x.name_course,
                    x.name_gr_course,
                    x.credits,
                    x.totalTheory,
                    x.totalPractice,
                    x.name_isCourse,
                    x.name_key_year_semester,
                    x.name_semester,
                    x.name_program,
                    time_open = x.window?.open_time,
                    time_close = x.window?.close_time,
                    is_open = x.window?.is_open ?? 0
                })
                .ToList();
            var totalRecords = raw.Count();
            return Ok(new
            {
                success = true,
                data = ListCourse,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }




        [HttpPost]
        [Route("danh-sach-giang-vien-viet-de-cuong-trong-mon-hoc")]
        public async Task<IActionResult> ListCourseByID([FromBody] SyllabusDTOs items)
        {
            var GetIDUser = await GetUserPermissionIDUser();
            var checkCourse = await db.Courses
                .Where(x => x.id_course == items.id_course)
                .ToListAsync();
            var ListData = new List<object>();
            foreach (var course in checkCourse)
            {
                var listData = await db.Syllabi
               .Where(x => x.id_teacherbysubjectNavigation.id_courseNavigation.code_course == course.code_course && x.id_teacherbysubjectNavigation.id_courseNavigation.name_course == course.name_course)
               .Select(x => new
               {
                   x.id_syllabus,
                   value = x.id_teacherbysubject,
                   code_status = x.id_status,
                   status = x.id_statusNavigation.name,
                   version = x.version,
                   is_open_edit_final = x.is_open_edit_final,
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
                       .ToList(),
                   is_approve_user = db.ApproveUserSyllabi.Where(g => g.id_syllabus == x.id_syllabus && g.id_user == GetIDUser).Select(g => g.is_approve).FirstOrDefault(),
               })
               .ToListAsync();

                ListData.AddRange(listData);
            }

            var window = db.OpenSyllabusWindowsCourses
                 .Where(g => g.id_course == items.id_course)
                 .Select(g => new
                 {
                     time_open = g.open_time,
                     time_close = g.close_time,
                     is_open = g.is_open

                 })
                 .FirstOrDefault();
            var GetNameCourse = await db.Courses
                .Where(x => x.id_course == items.id_course)
                .Select(x => x.name_course)
                .FirstOrDefaultAsync();
            var checkIsCreate = await db.TeacherBySubjects
                .Where(x => x.id_user == GetIDUser
                         && x.id_course == items.id_course)
                .Select(x => x.is_create_write)
                .FirstOrDefaultAsync();

            if (ListData.Any())
                return Ok(new { data = ListData, name_course = GetNameCourse, is_write = checkIsCreate, success = true });
            else

                return Ok(new { name_course = GetNameCourse, is_write = checkIsCreate, data = window, message = "Chưa có dữ liệu giảng viên viết đề cương", success = false });
        }

        [HttpPost]
        [Route("tao-moi-mau-de-cuong-cho-mon-hoc")]
        public async Task<IActionResult> TaoMoiMauDeCuong([FromBody] SyllabusDTOs items)
        {
            var userId = GetUserIdFromJWT();
            if (userId == null)
                return Unauthorized("Thiếu hoặc sai JWT token.");

            var teacherSubject = await db.TeacherBySubjects
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject)
                .Select(x => new
                {
                    x.id_teacherbysubject,
                    id_user = x.id_user,
                    faculty = x.id_courseNavigation.id_programNavigation.id_faculty
                })
                .FirstOrDefaultAsync();

            if (teacherSubject == null)
                return BadRequest(new { message = "Môn học không tồn tại.", success = false });

            var finalExist = await db.Syllabi
                 .AnyAsync(x => x.id_teacherbysubject == items.id_teacherbysubject
                             && x.create_by == userId
                             && x.id_status == 4);

            if (finalExist)
            {
                return Ok(new
                {
                    message = "Bạn đã có đề cương đã hoàn thiện, không thể tạo thêm.",
                    success = false
                });
            }

            var numericVersions = await db.Syllabi
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject
                         && x.create_by == userId)
                .Select(x => x.version)
                .ToListAsync();

            int nextVersion = numericVersions
                .Select(v => int.TryParse(v, out int n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var defaultTemplate = await db.SyllabusTemplates
                .Where(x => x.id_faculty == teacherSubject.faculty)
                .Select(x => x.template_json)
                .FirstOrDefaultAsync();

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var new_syllabus = new Syllabus
            {
                id_teacherbysubject = items.id_teacherbysubject,
                id_status = 1,
                version = nextVersion.ToString(),
                create_by = userId.Value,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                syllabus_json = defaultTemplate,
                is_open_edit_final = 0
            };

            db.Syllabi.Add(new_syllabus);
            await db.SaveChangesAsync();
            if (!await db.ApproveUserSyllabi
                .AnyAsync(x => x.id_user == teacherSubject.id_user && x.id_syllabus == new_syllabus.id_syllabus))
            {
                db.ApproveUserSyllabi.Add(new ApproveUserSyllabus
                {
                    id_user = teacherSubject.id_user,
                    id_syllabus = new_syllabus.id_syllabus,
                    is_approve = true,
                    is_key_user = true
                });

                await db.SaveChangesAsync();
            }
            var gvName = await GetUserPermissionNameCodeGV();

            db.Log_Syllabi.Add(new Log_Syllabus
            {
                id_syllabus = new_syllabus.id_syllabus,
                content_value = $"Giảng viên {gvName} vừa tạo mới đề cương phiên bản {nextVersion}",
                log_time = unixTimestamp
            });

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Tạo mẫu đề cương phiên bản {nextVersion} thành công.",
                success = true,
                version = nextVersion
            });
        }

        private int? GetUserIdFromJWT()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrWhiteSpace(token)) return null;

            var handler = new JwtSecurityTokenHandler();

            try
            {
                var jwt = handler.ReadJwtToken(token);
                var val = jwt.Claims.FirstOrDefault(c => c.Type == "id_users")?.Value;
                return int.TryParse(val, out var id) ? id : null;
            }
            catch
            {
                return null;
            }
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
        [HttpPost]
        [Route("delete-syllabus-1")]
        public async Task<IActionResult> DeleteSyllabusBy_1([FromBody] SyllabusDTOs items)
        {
            var CheckSyllabus = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .FirstOrDefaultAsync();

            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            db.Syllabi.Remove(CheckSyllabus);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa mẫu đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("rollback-syllabus")]
        public async Task<IActionResult> RollBackSyllabus([FromBody] SyllabusDTOs items)
        {
            var CheckSyllabus = await db.Syllabi
               .Where(x => x.id_syllabus == items.id_syllabus)
               .FirstOrDefaultAsync();

            if (CheckSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            CheckSyllabus.id_status = 1;
            CheckSyllabus.time_up = unixTimestamp;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = CheckSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa thu hồi đề cương về lại để chỉnh sửa với phiên bản {CheckSyllabus.version}",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thu hồi đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("preview-content-refund-syllabus")]
        public async Task<IActionResult> PreviewContentRefund([FromBody] SyllabusDTOs items)
        {
            var CheckContent = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => x.returned_content)
                .FirstOrDefaultAsync();
            if (CheckContent == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            return Ok(new { data = CheckContent, success = true });
        }
        [HttpPost]
        [Route("request-edit-syllabus")]
        public async Task<IActionResult> RequestEditSyllabus([FromBody] SyllabusDTOs items)
        {
            var GetNameGV = await GetUserPermissionNameCodeGV();
            if (string.IsNullOrEmpty(items.edit_content))
            {
                return Ok(new { message = "Không được bỏ trống lý do yêu cầu chỉnh sửa", success = false });
            }
            var checkSyllabus = await db.Syllabi
          .Include(x => x.id_teacherbysubjectNavigation)
              .ThenInclude(x => x.id_courseNavigation)
                  .ThenInclude(x => x.id_semesterNavigation)
          .Include(x => x.id_teacherbysubjectNavigation)
              .ThenInclude(x => x.id_courseNavigation)
                  .ThenInclude(x => x.id_key_year_semesterNavigation)
          .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);
            if (checkSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            checkSyllabus.is_open_edit_final = 1;
            checkSyllabus.edit_content = items.edit_content;
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = checkSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa yêu cầu mở chỉnh sửa bổ sung đề cương sau duyệt",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
            db.Notifications.Add(new Notification
            {
                id_user = null,
                id_program = checkSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_program,
                title = "Yêu cầu mở chỉnh sửa đề cương",
                message =
                 $"Giảng viên {GetNameGV} đã gửi yêu cầu mở chỉnh sửa đề cương môn " +
                 $"{checkSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.code_course} – " +
                 $"{checkSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.name_course} – " +
                 $"{checkSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_semesterNavigation?.name_semester} – " +
                 $"{checkSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semesterNavigation?.name_key_year_semester}. " +
                 $"Vui lòng xem xét và phê duyệt.",
                type = "request_edit_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/ctdt/danh-sach-de-cuong-can-duyet"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Gửi yêu cầu mở chỉnh sửa bổ sung thành công", success = true });
        }
        [HttpPost]
        [Route("cancer-edit-syllabus")]
        public async Task<IActionResult> CancerSyllabusEdit([FromBody] SyllabusDTOs items)
        {
            var checkSyllabus = await db.Syllabi
               .Where(x => x.id_syllabus == items.id_syllabus)
               .FirstOrDefaultAsync();
            if (checkSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            checkSyllabus.is_open_edit_final = 0;
            checkSyllabus.edit_content = null;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = checkSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa thu hồi yêu cầu chỉnh sửa đề cương sau duyệt",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thu hồi đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("clone-syllabus")]
        public async Task<IActionResult> CloneSyllabus([FromBody] SyllabusDTOs items)
        {
            var userId = GetUserIdFromJWT();
            if (userId == null)
                return Unauthorized("Thiếu hoặc sai JWT token.");

            var teacherSubject = await db.TeacherBySubjects
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject)
                .Select(x => new
                {
                    x.id_teacherbysubject,
                    id_user = x.id_user,
                    faculty = x.id_courseNavigation.id_programNavigation.id_faculty
                })
                .FirstOrDefaultAsync();

            if (teacherSubject == null)
                return BadRequest(new { message = "Môn học không tồn tại.", success = false });


            var numericVersions = await db.Syllabi
                .Where(x => x.id_teacherbysubject == items.id_teacherbysubject
                         && x.create_by == userId)
                .Select(x => x.version)
                .ToListAsync();

            int nextVersion = numericVersions
                .Select(v => int.TryParse(v, out int n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var defaultTemplate = await db.SyllabusTemplates
                .Where(x => x.id_faculty == teacherSubject.faculty)
                .Select(x => x.template_json)
                .FirstOrDefaultAsync();

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var checkSyllabus = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus).FirstOrDefaultAsync();
            var new_syllabus = new Syllabus
            {
                id_teacherbysubject = items.id_teacherbysubject,
                id_status = 8,
                version = nextVersion.ToString(),
                create_by = userId.Value,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                syllabus_json = checkSyllabus.syllabus_json,
                is_open_edit_final = 0
            };

            db.Syllabi.Add(new_syllabus);
            await db.SaveChangesAsync();
            if (!await db.ApproveUserSyllabi
                .AnyAsync(x => x.id_user == teacherSubject.id_user && x.id_syllabus == new_syllabus.id_syllabus))
            {
                db.ApproveUserSyllabi.Add(new ApproveUserSyllabus
                {
                    id_user = teacherSubject.id_user,
                    id_syllabus = new_syllabus.id_syllabus,
                    is_approve = true,
                    is_key_user = true
                });

                await db.SaveChangesAsync();
            }
            var gvName = await GetUserPermissionNameCodeGV();

            db.Log_Syllabi.Add(new Log_Syllabus
            {
                id_syllabus = new_syllabus.id_syllabus,
                content_value = $"Giảng viên {gvName} vừa Clone phiên bản đề cương {nextVersion}",
                log_time = unixTimestamp
            });

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Clone mẫu đề cương thành công",
                success = true,
                version = nextVersion
            });
        }

        [HttpPost]
        [Route("request-write-course-by-user")]
        public async Task<IActionResult> RequestUserSyllabusEdit([FromBody] SyllabusDTOs items)
        {
            var GetIDUser = await GetUserPermissionIDUser();
            var GetNameGV = await GetUserPermissionNameCodeGV();

            if (!await db.ApproveUserSyllabi
                .AnyAsync(x => x.id_user == GetIDUser && x.id_syllabus == items.id_syllabus))
            {
                db.ApproveUserSyllabi.Add(new ApproveUserSyllabus
                {
                    id_user = GetIDUser,
                    id_syllabus = items.id_syllabus,
                    is_approve = false,
                    is_key_user = false,
                    is_refuse = false,
                    time_request = unixTimestamp,
                    time_accept_request = null
                });

                await db.SaveChangesAsync();
            }
            var GetSyllabus = await db.Syllabi.FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);
            db.Notifications.Add(new Notification
            {
                id_user = GetSyllabus.id_teacherbysubjectNavigation.id_user,
                id_program = null,
                title = "Yêu cầu tham gia viết đề cương",
                message =
                    $"Giảng viên {GetNameGV} đã gửi yêu cầu tham gia viết đề cương môn học " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.code_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.name_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_semesterNavigation?.name_semester} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semesterNavigation?.name_key_year_semester}. " +
                    $"Vui lòng xem xét và phê duyệt.",
                type = "request_write_course_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Gửi yêu cầu tham gia thành công", success = true });
        }

        [HttpPost]
        [Route("loads-list-join-write-course")]
        public async Task<IActionResult> LoadListJoinWriteCourse([FromBody] SyllabusDTOs items)
        {
            var ListData = await db.ApproveUserSyllabi
                .Where(x => x.id_syllabus == items.id_syllabus && x.is_key_user == false)
                .Select(x => new
                {
                    x.id_ApproveUserSyllabus,
                    code_civil = db.CivilServants.Where(g => g.email == x.id_userNavigation.email).Select(g => g.code_civilSer).FirstOrDefault(),
                    name_civil = db.CivilServants.Where(g => g.email == x.id_userNavigation.email).Select(g => g.fullname_civilSer).FirstOrDefault(),
                    email = x.id_userNavigation.email,
                    name_program = db.CivilServants.Where(g => g.email == x.id_userNavigation.email).Select(g => g.id_programNavigation.name_program).FirstOrDefault(),
                    x.is_approve,
                    x.is_key_user,
                    x.is_refuse,
                    x.time_request,
                    x.time_accept_request,
                })
                .ToListAsync();
            if (ListData.Count > 0)
            {
                return Ok(new { data = ListData, success = true });
            }
            else
            {
                return Ok(new { message = "Chưa có dữ liệu", success = false });
            }
        }
        [HttpPost]
        [Route("accept-join-write-course")]
        public async Task<IActionResult> AcceptJoinWriteCourse([FromBody] ApproveUserSyllabusDTOs items)
        {
            var GetIDUser = await GetUserPermissionIDUser();
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var checkApprove = await db.ApproveUserSyllabi.Where(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus).FirstOrDefaultAsync();
            if (checkApprove == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin", success = false });
            }
            checkApprove.is_approve = true;
            checkApprove.is_refuse = false;
            checkApprove.time_accept_request = unixTimestamp;

            var GetSyllabus = await db.Syllabi.FirstOrDefaultAsync(x => x.id_syllabus == checkApprove.id_syllabus);
            db.Notifications.Add(new Notification
            {
                id_user = checkApprove.id_user,
                id_program = null,
                title = "Duyệt yêu cầu tham gia viết đề cương",
                message =
                    $"Giảng viên {GetNameGV} đã duyệt cầu tham gia viết đề cương môn học " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.code_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.name_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_semesterNavigation?.name_semester} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semesterNavigation?.name_key_year_semester}. ",
                type = "accept_write_course_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Duyệt yêu cầu thành công", success = true });
        }

        [HttpPost]
        [Route("reject-join-write-course")]
        public async Task<IActionResult> RejectJoinWriteCourse([FromBody] ApproveUserSyllabusDTOs items)
        {
            var GetIDUser = await GetUserPermissionIDUser();
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var checkApprove = await db.ApproveUserSyllabi.Where(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus).FirstOrDefaultAsync();
            if (checkApprove == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin", success = false });
            }
            checkApprove.is_approve = false;
            checkApprove.is_refuse = true;
            checkApprove.time_accept_request = unixTimestamp;
            var GetSyllabus = await db.Syllabi.FirstOrDefaultAsync(x => x.id_syllabus == checkApprove.id_syllabus);
            db.Notifications.Add(new Notification
            {
                id_user = checkApprove.id_user,
                id_program = null,
                title = "Từ chối yêu cầu tham gia viết đề cương",
                message =
                    $"Giảng viên {GetNameGV} đã từ chối yêu cầu tham gia viết đề cương môn học " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.code_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.name_course} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_semesterNavigation?.name_semester} – " +
                    $"{GetSyllabus.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semesterNavigation?.name_key_year_semester}.",
                type = "reject_write_course_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });
            await db.SaveChangesAsync();
            return Ok(new { message = "Từ chối yêu cầu thành công", success = true });
        }
        [HttpPost]
        [Route("remove-join-write-course")]
        public async Task<IActionResult> RemoveJoinWriteCourse([FromBody] ApproveUserSyllabusDTOs items)
        {
            var GetNameGV = await GetUserPermissionNameCodeGV();

            var checkApprove = await db.ApproveUserSyllabi
                .Include(x => x.id_syllabusNavigation)
                    .ThenInclude(s => s.id_teacherbysubjectNavigation)
                        .ThenInclude(t => t.id_courseNavigation)
                            .ThenInclude(c => c.id_semesterNavigation)
                .Include(x => x.id_syllabusNavigation)
                    .ThenInclude(s => s.id_teacherbysubjectNavigation)
                        .ThenInclude(t => t.id_courseNavigation)
                            .ThenInclude(c => c.id_key_year_semesterNavigation)
                .FirstOrDefaultAsync(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus);

            if (checkApprove == null)
                return Ok(new { message = "Không tìm thấy thông tin", success = false });

            var syllabus = checkApprove.id_syllabusNavigation;
            var tbsNav = syllabus?.id_teacherbysubjectNavigation;
            var course = tbsNav?.id_courseNavigation;

            if (tbsNav != null)
            {
                var checkTeacherBySyllabus = await db.TeacherBySubjects
                    .FirstOrDefaultAsync(x =>
                        x.id_course == tbsNav.id_course &&
                        x.id_user == checkApprove.id_user);

                if (checkTeacherBySyllabus != null)
                {
                    db.TeacherBySubjects.Remove(checkTeacherBySyllabus);
                }
            }

            db.Notifications.Add(new Notification
            {
                id_user = checkApprove.id_user,
                id_program = null,
                title = "Loại khỏi danh sách viết đề cương",
                message =
                    $"Giảng viên {GetNameGV} đã loại bạn khỏi danh sách tham gia viết đề cương môn học " +
                    $"{course.code_course} – {course.name_course} – " +
                    $"{course.id_semesterNavigation?.name_semester} – " +
                    $"{course.id_key_year_semesterNavigation?.name_key_year_semester}.",
                type = "remove_write_course_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });

            db.ApproveUserSyllabi.Remove(checkApprove);

            await db.SaveChangesAsync();

            return Ok(new { message = "Loại thành viên thành công", success = true });
        }

        [HttpPost]
        [Route("phan-quyen-gv-vao-phu-viet-de-cuong")]
        public async Task<IActionResult> PhanQuyenVietTiepDeCuong([FromBody] CivilServantsDTOs items)
        {
            var GetNameGV = await GetUserPermissionNameCodeGV();

            var idProgram = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_program)
                .FirstOrDefaultAsync();

            var checkCivil = await db.CivilServants
                .FirstOrDefaultAsync(x => x.code_civilSer == items.code_civilSer && x.id_program == idProgram);

            if (checkCivil == null)
                return Ok(new { message = "Giảng viên này không tồn tại hoặc sai mã, vui lòng kiểm tra lại", success = false });

            var checkUser = await db.Users.FirstOrDefaultAsync(x => x.email == checkCivil.email);

            if (checkUser == null)
            {
                checkUser = new User
                {
                    email = checkCivil.email,
                    time_cre = unixTimestamp,
                    time_up = unixTimestamp,
                    id_type_users = 4,
                    status = 1
                };
                db.Users.Add(checkUser);
                await db.SaveChangesAsync();
            }
            else
            {
                checkUser.id_type_users = 4;
                checkUser.status = 1;
                checkUser.time_up = unixTimestamp;
                await db.SaveChangesAsync();
            }

            var blockTypes = new int[] { 2, 3, 5 };
            var isManager = blockTypes.Contains(checkUser.id_type_users ?? 0);

            if (isManager)
                return Ok(new { message = "Giảng viên này thuộc cấp quyền quản lý, không thể thêm", success = false });

            var exist = await db.ApproveUserSyllabi
                .AnyAsync(x => x.id_user == checkUser.id_users && x.id_syllabus == items.id_syllabus);

            if (exist)
                return Ok(new { message = "Giảng viên này đã được phân quyền vào đề cương này", success = false });

            db.ApproveUserSyllabi.Add(new ApproveUserSyllabus
            {
                id_user = checkUser.id_users,
                id_syllabus = items.id_syllabus,
                is_approve = true,
                is_key_user = false,
                is_refuse = false,
                time_request = unixTimestamp,
                time_accept_request = unixTimestamp
            });

            await db.SaveChangesAsync();

            var syllabus = await db.Syllabi
                .Include(s => s.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_semesterNavigation)
                .Include(s => s.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                        .ThenInclude(c => c.id_key_year_semesterNavigation)
                .FirstOrDefaultAsync(s => s.id_syllabus == items.id_syllabus);

            if (syllabus == null)
                return Ok(new { message = "Không tìm thấy đề cương", success = false });

            var course = syllabus.id_teacherbysubjectNavigation.id_courseNavigation;

            var existTBS = await db.TeacherBySubjects
                .FirstOrDefaultAsync(x => x.id_course == course.id_course && x.id_user == checkUser.id_users);

            if (existTBS == null)
            {
                db.TeacherBySubjects.Add(new TeacherBySubject
                {
                    id_user = checkUser.id_users,
                    id_course = course.id_course,
                    is_create_write = false
                });
            }

            db.Notifications.Add(new Notification
            {
                id_user = checkUser.id_users,
                id_program = null,
                title = "Phân công phụ viết đề cương",
                message =
                    $"Giảng viên {GetNameGV} đã phân công bạn tham gia phụ viết đề cương môn " +
                    $"{course.code_course} – {course.name_course} – " +
                    $"{course.id_semesterNavigation?.name_semester} – " +
                    $"{course.id_key_year_semesterNavigation?.name_key_year_semester}.",
                type = "permission_write_course_syllabus",
                create_time = unixTimestamp,
                is_read = false,
                link = "/gv-de-cuong/danh-sach-de-cuong-duoc-phan-cong"
            });

            await db.SaveChangesAsync();

            return Ok(new { message = "Phân quyền giảng viên phụ viết đề cương thành công", success = true });
        }


    }
}
