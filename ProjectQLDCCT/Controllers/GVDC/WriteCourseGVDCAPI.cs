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

        [HttpGet]
        [Route("loads-danh-sach-de-cuong-can-soan")]
        public async Task<IActionResult> LoadCourseByPermission()
        {
            var List = await GetUserPermissionCourse();
            var raw = await db.TeacherBySubjects
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
                      name_isCourse = x.id_courseNavigation.id_isCourseNavigation.name,
                      name_key_year_semester = x.id_courseNavigation.id_key_year_semesterNavigation.name_key_year_semester,
                      name_semester = x.id_courseNavigation.id_semesterNavigation.name_semester,
                      name_program = x.id_courseNavigation.id_programNavigation.name_program,

                      window = db.OpenSyllabusWindowsCourses
                          .Where(g => g.id_course == x.id_course)
                          .Select(g => new { g.open_time, g.close_time, g.is_open })
                          .FirstOrDefault()
                  })
                  .ToListAsync();

            var ListCourse = raw
                .GroupBy(x => x.id_course)
                .Select(g => g.First())
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
                    is_open = x.window?.is_open
                })
                .ToList();

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
                     time_close = g.close_time
                 })
                 .FirstOrDefault();
            var GetNameCourse = await db.Courses
                .Where(x => x.id_course == items.id_course)
                .FirstOrDefaultAsync();
            var checkIsCreate = await db.TeacherBySubjects
                .Where(x => x.id_user == GetIDUser
                         && x.id_course == items.id_course)
                .Select(x => x.is_create_write)
                .FirstOrDefaultAsync();

            if (ListData.Any())
                return Ok(new { data = ListData, name_course = GetNameCourse.name_course, is_write = checkIsCreate, success = true });
            else

                return Ok(new { name_course = GetNameCourse, data = window, message = "Chưa có dữ liệu giảng viên viết đề cương", success = false });
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
            if (string.IsNullOrEmpty(items.edit_content))
            {
                return Ok(new { message = "Không được bỏ trống lý do yêu cầu chỉnh sửa", success = false });
            }
            var checkSyllabus = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .FirstOrDefaultAsync();
            if (checkSyllabus == null)
                return Ok(new { message = "Không tìm thấy thông tin đề cương", success = false });

            checkSyllabus.is_open_edit_final = 1;
            checkSyllabus.edit_content = items.edit_content;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = checkSyllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa yêu cầu mở chỉnh sửa bổ sung đề cương sau duyệt",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
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
        [Route("request-write-course-by-user")]
        public async Task<IActionResult> RequestUserSyllabusEdit([FromBody] SyllabusDTOs items)
        {
            var GetIDUser = await GetUserPermissionIDUser();
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
            var checkApprove = await db.ApproveUserSyllabi.Where(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus).FirstOrDefaultAsync();
            if (checkApprove == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin", success = false });
            }
            checkApprove.is_approve = true;
            checkApprove.is_refuse = false;
            checkApprove.time_accept_request = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Duyệt yêu cầu thành công", success = true });
        }

        [HttpPost]
        [Route("reject-join-write-course")]
        public async Task<IActionResult> RejectJoinWriteCourse([FromBody] ApproveUserSyllabusDTOs items)
        {
            var checkApprove = await db.ApproveUserSyllabi.Where(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus).FirstOrDefaultAsync();
            if (checkApprove == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin", success = false });
            }
            checkApprove.is_approve = false;
            checkApprove.is_refuse = true;
            checkApprove.time_accept_request = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Từ chối yêu cầu thành công", success = true });
        }
        [HttpPost]
        [Route("remove-join-write-course")]
        public async Task<IActionResult> RemoveJoinWriteCourse([FromBody] ApproveUserSyllabusDTOs items)
        {
            var checkApprove = await db.ApproveUserSyllabi.Where(x => x.id_ApproveUserSyllabus == items.id_ApproveUserSyllabus).FirstOrDefaultAsync();
            if (checkApprove == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin", success = false });
            }
            db.ApproveUserSyllabi.Remove(checkApprove);
            await db.SaveChangesAsync();
            return Ok(new { message = "Loại thành viên thành công", success = true });
        }

        [HttpPost]
        [Route("phan-quyen-gv-vao-phu-viet-de-cuong")]
        public async Task<IActionResult> PhanQuyenVietTiepDeCuong([FromBody] CivilServantsDTOs items)
        {
            var GetProgram = await db.Syllabi.Where(x => x.id_syllabus == items.id_syllabus).Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_program).FirstOrDefaultAsync();
            var checkCivil = await db.CivilServants
                .FirstOrDefaultAsync(x => x.code_civilSer == items.code_civilSer && GetProgram == x.id_program);
            if (checkCivil == null)
                return Ok(new { message = "Giảng viên này không tồn tại hoặc sai mã, vui lòng kiểm tra lại", success = false });

            var checkUser = await db.Users
                .FirstOrDefaultAsync(x => x.email == checkCivil.email);

            if (checkUser == null)
            {
                var newUser = new User
                {
                    email = checkCivil.email,
                    time_cre = unixTimestamp,
                    time_up = unixTimestamp,
                    id_type_users = 4,
                    status = 1
                };
                db.Users.Add(newUser);
                await db.SaveChangesAsync();
                checkUser = newUser;
            }
            else
            {
                checkUser.id_type_users = 4;
                checkUser.status = 1;
                checkUser.time_up = unixTimestamp;
                await db.SaveChangesAsync();
            }
            var newlistint = new int[] { 2, 3, 5 };
            var CheckType = await db.Users.Where(x => x.id_users == checkUser.id_users && newlistint.Contains(x.id_type_users ?? 0)).FirstOrDefaultAsync();
            if (CheckType != null)
                return Ok(new { message = "Giảng viên này thuộc cấp quyền quản lý, không thể thêm", success = false });
            var existCourse = await db.ApproveUserSyllabi
                .FirstOrDefaultAsync(x => x.id_user == checkUser.id_users && x.id_syllabus == items.id_syllabus);

            if (existCourse != null)
                return Ok(new { message = "Giảng viên này đã được phân quyền vào đề cương môn học này", success = false });

            var newRecord = new ApproveUserSyllabus
            {
                id_user = checkUser.id_users,
                id_syllabus = items.id_syllabus,
                is_approve = true,
                is_key_user = false,
                is_refuse = false,
                time_accept_request = unixTimestamp,
                time_request = unixTimestamp
            };
            db.ApproveUserSyllabi.Add(newRecord);
            await db.SaveChangesAsync();

            return Ok(new { message = "Phân quyền giảng viên viết đề cương thành công", success = true });
        }
    }
}
