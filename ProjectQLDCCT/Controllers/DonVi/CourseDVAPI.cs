using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Authorize(Policy = "DonVi")]
    [Route("api/donvi/course")]
    [ApiController]
    public class CourseDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CourseDVAPI(QLDCContext _db)
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
        [Route("loads-ctdt-by-dv")]
        public async Task<IActionResult> LoadsCTDTByDV()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var ListCTDT = await db.TrainingPrograms
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id_program,
                    x.name_program
                })
                .ToListAsync();
            return Ok(ListCTDT);
        }
        [HttpGet]
        [Route("load-select-chuc-nang-course")]
        public async Task<IActionResult> LoadOptionSelectedUpdate()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var LoadListGroupHocPhan = await db.Group_Courses
                .Select(x => new
                {
                    value = x.id_gr_course,
                    text = x.name_gr_course
                })
                .ToListAsync();
            var LoadIsHocPhan = await db.IsCourses
                .Select(x => new
                {
                    value = x.id,
                    text = x.name,
                })
                .ToListAsync();
            var LoadIsSemester = await db.Semesters
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.code_semester)
                .Select(x => new
                {
                    value = x.id_semester,
                    text = x.code_semester + " - " + x.name_semester
                })
                .ToListAsync();
            var LoadKeyYearSemester = await db.KeyYearSemesters
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.code_key_year_semester)
                .Select(x => new
                {
                    value = x.id_key_year_semester,
                    text = x.code_key_year_semester + " - " + x.name_key_year_semester
                })
                .ToListAsync();
            return Ok(new
            {
                nhom_hoc_phan = LoadListGroupHocPhan,
                is_hoc_phan = LoadIsHocPhan,
                Semester = LoadIsSemester,
                KeyYearSemester = LoadKeyYearSemester,
            });
        }
        [HttpPost]
        [Route("loads-danh-sach-mon-hoc-thuoc-don-vi")]
        public async Task<IActionResult> LoadCourse([FromBody] CourseDTOs items)
        {

            var query = db.Courses
                .AsNoTracking()
                .Where(x => items.id_program == x.id_program);

            if (items.id_gr_course > 0)
            {
                query = query.Where(x => x.id_gr_course == items.id_gr_course);
            }

            if (items.id_isCourse > 0)
            {
                query = query.Where(x => x.id_isCourse == items.id_isCourse);
            }
            if (items.id_program > 0)
            {
                query = query.Where(x => x.id_program == items.id_program);
            }
            if (items.id_key_year_semester > 0)
            {
                query = query.Where(x => x.id_key_year_semester == items.id_key_year_semester);
            }
            if (items.id_semester > 0)
            {
                query = query.Where(x => x.id_semester == items.id_semester);
            }
            if (!string.IsNullOrEmpty(items.searchTerm))
            {
                string keyword = items.searchTerm.ToLower();
                query = query.Where(x =>
                x.code_course.ToLower().Contains(keyword) ||
                x.name_course.ToLower().Contains(keyword) ||
                x.id_gr_courseNavigation.name_gr_course.ToLower().Contains(keyword) ||
                x.credits.ToString().Contains(keyword) ||
                x.totalTheory.ToString().Contains(keyword) ||
                x.id_programNavigation.name_program.ToLower().Contains(keyword));
            }
            var totalRecords = await query.CountAsync();

            var data = await query
                .OrderByDescending(x => x.id_course)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_course,
                    x.code_course,
                    x.name_course,
                    name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                    x.credits,
                    x.totalTheory,
                    x.totalPractice,
                    x.id_programNavigation.name_program,
                    x.time_cre,
                    x.time_up,
                    name = x.id_isCourseNavigation.name,
                    name_semester = x.id_semesterNavigation.code_semester + " - " + x.id_semesterNavigation.name_semester,
                    name_key_year_semester = x.id_key_year_semesterNavigation.code_key_year_semester + " - " + x.id_key_year_semesterNavigation.name_key_year_semester,
                    is_syllabus = db.Syllabi.Any(g => g.id_teacherbysubjectNavigation.id_course == x.id_course && g.id_status == 4)
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }

        [HttpPost]
        [Route("loads-danh-sach-giang-vien-viet-de-cuong")]
        public async Task<IActionResult> LoadListGvVietDeCuong([FromBody] ApproveUserSyllabusDTOs items)
        {
            var checkSyllabus = await db.Syllabi
                .Include(x => x.id_teacherbysubjectNavigation)
                .FirstOrDefaultAsync(x => x.id_teacherbysubjectNavigation.id_course == items.id_course);

            if (checkSyllabus == null)
            {
                return Ok(new { message = "Chưa có danh sách giảng viên phụ trách viết đề cương trong môn học này", success = false });
            }

            var getList = await db.ApproveUserSyllabi
                .Where(x => x.id_syllabus == checkSyllabus.id_syllabus)
                .Select(x => new
                {
                    x.id_ApproveUserSyllabus,

                    email = x.id_userNavigation != null ? x.id_userNavigation.email : null,

                    civil = db.CivilServants
                        .Where(g => g.email == x.id_userNavigation.email)
                        .Select(g => new
                        {
                            g.code_civilSer,
                            g.fullname_civilSer,
                            program_name = g.id_programNavigation.name_program
                        })
                        .FirstOrDefault(),

                    x.is_approve,
                    x.is_key_user,
                    x.is_refuse,
                    x.time_request,
                    x.time_accept_request,
                })
                .ToListAsync();

            var result = getList.Select(x => new
            {
                x.id_ApproveUserSyllabus,
                code_civil = x.civil?.code_civilSer ?? "",
                name_civil = x.civil?.fullname_civilSer ?? "",
                email = x.email ?? "",
                name_program = x.civil?.program_name ?? "",
                x.is_approve,
                x.is_key_user,
                x.is_refuse,
                x.time_request,
                x.time_accept_request
            }).ToList();

            if (result.Count == 0)
            {
                return Ok(new { message = "Chưa có dữ liệu", success = false });
            }

            return Ok(new { data = result, success = true });
        }

        [HttpPost]
        [Route("loads-mon-hoc-dang-hoc-ky")]
        public async Task<IActionResult> LoadHocPhan([FromBody] CourseDTOs items)
        {
            if (items.id_key_year_semester == 0)
                return Ok(new { message = "Vui lòng chọn khóa học để có thể sử dụng chức năng này", success = false });
            var CheckCTDT = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.id_program == items.id_program);
            var checkKeySemester = await db.KeyYearSemesters
                .FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);

            int? filterYear = null;
            if (checkKeySemester != null && !string.IsNullOrEmpty(checkKeySemester.code_key_year_semester))
            {
                var suffix = new string(checkKeySemester.code_key_year_semester
                    .Where(char.IsDigit)
                    .ToArray());
                if (int.TryParse(suffix, out int yearSuffix))
                {
                    filterYear = 2000 + yearSuffix;
                }
            }
            var LoadSemesterQuery = db.Semesters
                .Where(x => CheckCTDT.id_faculty == x.id_faculty);

            if (filterYear.HasValue)
            {
                LoadSemesterQuery = LoadSemesterQuery
                    .Where(x => Convert.ToInt32(x.code_semester.Substring(0, 4)) >= filterYear.Value);
            }

            var LoadSemester = await LoadSemesterQuery
                .OrderBy(x => x.code_semester)
                .ToListAsync();

            var ListData = new List<object>();

            foreach (var semester in LoadSemester)
            {
                var loadCourse = await db.Courses
                    .Where(x =>
                        x.id_semester == semester.id_semester &&
                        x.id_key_year_semester == items.id_key_year_semester &&
                        x.id_program == items.id_program)
                    .Select(x => new
                    {
                        x.id_course,
                        x.code_course,
                        x.name_course,
                        name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                        name_isCourse = x.id_isCourseNavigation.name,
                        x.totalPractice,
                        x.totalTheory,
                        x.credits,
                        count_syllabus = x.TeacherBySubjects.Count(),
                        time_open = db.OpenSyllabusWindowsCourses.Where(g => g.id_course == x.id_course).Select(g => g.open_time).FirstOrDefault(),
                        time_close = db.OpenSyllabusWindowsCourses.Where(g => g.id_course == x.id_course).Select(g => g.close_time).FirstOrDefault(),
                        is_syllabus = db.Syllabi.Any(g => g.id_teacherbysubjectNavigation.id_course == x.id_course && g.id_status == 4)
                    })
                    .ToListAsync();
                if (loadCourse.Count > 0)
                {
                    ListData.Add(new
                    {
                        id_se = semester.id_semester,
                        name_se = semester.name_semester,
                        code_se = semester.code_semester,
                        course = loadCourse
                    });
                }
            }
            var listCourse = await db.Courses
          .Where(x => x.id_key_year_semester == items.id_key_year_semester
                   && x.id_program == items.id_program)
          .Select(x => x.id_course)
          .ToListAsync();

            int totalCourse = listCourse.Count;

            int totalSyllabus = await db.Syllabi
                .CountAsync(g => listCourse.Contains(g.id_teacherbysubjectNavigation.id_course ?? 0)
                              && g.id_status == 4);
            return Ok(new { data = ListData, total_course = totalCourse, total_syllabus = totalSyllabus, message = "Lọc dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("them-moi-mon-hoc")]
        public async Task<IActionResult> ThemMoiMonHoc([FromBody] CourseDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_course))
            {
                return Ok(new { message = "Không được bỏ trống trường Mã môn học", success = false });
            }
            if (string.IsNullOrEmpty(items.name_course))
            {
                return Ok(new { message = "Không được bỏ trống trường Tên môn học", success = false });
            }
            var CheckMonHoc = await db.Courses.Where(x => items.id_program == x.id_program &&
            x.code_course.ToLower().Trim() == items.code_course.ToLower().Trim() &&
            x.name_course.ToLower().Trim() == items.name_course.ToLower().Trim())
            .FirstOrDefaultAsync();
            if (CheckMonHoc != null)
            {
                return Ok(new { message = "Môn học này đã tồn tại, vui lòng kiểm tra lại", success = true });
            }
            var new_record = new Course
            {
                id_program = items.id_program,
                code_course = items.code_course,
                name_course = items.name_course,
                id_gr_course = items.id_gr_course,
                credits = items.credits,
                totalPractice = items.totalPractice,
                id_semester = items.id_semester,
                id_key_year_semester = items.id_key_year_semester,
                totalTheory = items.totalTheory,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                id_isCourse = items.id_isCourse,
            };
            db.Courses.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-mon-hoc")]
        public async Task<IActionResult> InfoMonHoc([FromBody] CourseDTOs items)
        {
            var checkCourse = await db.Courses
                .Where(x => x.id_course == items.id_course)
                .Select(x => new
                {
                    x.id_course,
                    x.id_program,
                    x.code_course,
                    x.name_course,
                    x.id_gr_course,
                    x.credits,
                    x.totalPractice,
                    x.id_key_year_semester,
                    x.id_semester,
                    x.totalTheory,
                    x.id_isCourse
                })
                .FirstOrDefaultAsync();
            if (checkCourse == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin môn học", success = false });
            }
            return Ok(new { data = checkCourse, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-mon-hoc")]
        public async Task<IActionResult> UpdateMonHoc([FromBody] CourseDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_course))
            {
                return Ok(new { message = "Không được bỏ trống trường Mã môn học", success = false });
            }
            if (string.IsNullOrEmpty(items.name_course))
            {
                return Ok(new { message = "Không được bỏ trống trường Tên môn học", success = false });
            }
            var checkCourse = await db.Courses.FirstOrDefaultAsync(x => x.id_course == items.id_course);
            if (checkCourse == null)
            {
                return Ok(new { message = "Không tìm thấy dữ liệu Môn học", success = false });
            }

            checkCourse.code_course = items.code_course;
            checkCourse.name_course = items.name_course;
            checkCourse.credits = items.credits;
            checkCourse.totalTheory = items.totalTheory;
            checkCourse.totalPractice = items.totalPractice;
            checkCourse.id_gr_course = items.id_gr_course;
            checkCourse.id_isCourse = items.id_isCourse;
            checkCourse.id_semester = items.id_semester;
            checkCourse.id_key_year_semester = items.id_key_year_semester;
            checkCourse.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-mon-hoc")]
        public async Task<IActionResult> DeleteMonHoc([FromBody] CourseDTOs items)
        {
            var CheckCourse = await db.Courses.FirstOrDefaultAsync(x => x.id_course == items.id_course);
            if (CheckCourse == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin Môn học", success = false });
            }

            db.Courses.Remove(CheckCourse);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("log-hoat-dong-de-cuong")]
        public async Task<IActionResult> LoadLogSyllabus([FromBody] LogSyllabusDTOs items)
        {
            var LoadLogOperation = await db.Log_Syllabi
                .Where(x => x.id_syllabusNavigation.id_teacherbysubjectNavigation.id_course == items.id_course)
                .Select(x => new
                {
                    x.content_value,
                    x.log_time
                })
                .ToListAsync();
            return Ok(LoadLogOperation);
        }

        [HttpPost("upload-excel-danh-sach-mon-hoc")]
        public async Task<IActionResult> UploadExcelMonHoc(IFormFile file)
        {
            var GetFaculty = await GetUserPermissionFaculties();
            if (file == null || file.Length == 0)
                return Ok(new { message = "Vui lòng chọn file Excel.", success = false });

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return Ok(new { message = "Chỉ hỗ trợ upload file Excel.", success = false });
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                var ID = Request.Form["id_program"];
                int IdProgram = int.Parse(ID);
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                        if (worksheet == null)
                        {
                            return Ok(new { message = "Không tìm thấy worksheet trong file Excel", success = false });
                        }


                        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                        {
                            var ma_mh = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_mh = worksheet.Cells[row, 3].Text?.Trim();
                            var nhom_mh = worksheet.Cells[row, 4].Text?.Trim();
                            var tinchi = worksheet.Cells[row, 5].Text?.Trim();
                            var tongLyThuyet = worksheet.Cells[row, 6].Text?.Trim();
                            var tongThucHanh = worksheet.Cells[row, 7].Text?.Trim();
                            var ThuocHocPhan = worksheet.Cells[row, 8].Text?.Trim();
                            var ThuocHocKy = worksheet.Cells[row, 9].Text?.Trim();
                            var ThuocKhoaHoc = worksheet.Cells[row, 10].Text?.Trim();
                            var CheckNhomMH = await db.Group_Courses.Where(x => x.name_gr_course.ToLower().Trim() == nhom_mh.ToLower().Trim()).FirstOrDefaultAsync();
                            if (!string.IsNullOrWhiteSpace(nhom_mh) && CheckNhomMH == null)
                            {
                                return Ok(new { message = $"Tên nhóm môn học ${nhom_mh} không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại", success = false });
                            }
                            var CheckIsHocPhan = await db.IsCourses.FirstOrDefaultAsync(x => x.name.ToLower().Trim() == ThuocHocPhan.ToLower().Trim());
                            if (!string.IsNullOrWhiteSpace(ThuocHocPhan) && CheckIsHocPhan == null)
                            {
                                return Ok(new { message = $"Là học phần ${ThuocHocPhan} không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại", success = false });
                            }
                            var CheckKhoaHoc = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.name_key_year_semester.ToLower().Trim() == ThuocKhoaHoc.ToLower().Trim() && GetFaculty.Contains(x.id_faculty ?? 0));
                            if (!string.IsNullOrWhiteSpace(ThuocKhoaHoc) && CheckKhoaHoc == null)
                            {
                                return Ok(new { message = $"Khóa học ${ThuocKhoaHoc} không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại", success = false });
                            }
                            var CheckHocKy = await db.Semesters.FirstOrDefaultAsync(x => x.name_semester.ToLower().Trim() == ThuocHocKy.ToLower().Trim() && GetFaculty.Contains(x.id_faculty ?? 0));
                            if (!string.IsNullOrWhiteSpace(ThuocHocKy) && CheckHocKy == null)
                            {
                                return Ok(new { message = $"Học kỳ ${ThuocHocKy} không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại", success = false });
                            }
                            var check_mh = await db.Courses
                                .FirstOrDefaultAsync(x =>
                                    x.code_course.ToLower().Trim() == ma_mh.ToLower() &&
                                    x.name_course.ToLower().Trim() == ten_mh.ToLower() &&
                                    x.id_key_year_semester == CheckKhoaHoc.id_key_year_semester &&
                                    x.id_semester == CheckHocKy.id_semester
                                    );
                            if (check_mh == null)
                            {
                                check_mh = new Course
                                {
                                    code_course = string.IsNullOrWhiteSpace(ma_mh) ? null : ma_mh.ToUpper(),
                                    name_course = ten_mh,
                                    id_gr_course = string.IsNullOrWhiteSpace(nhom_mh) ? null : CheckNhomMH.id_gr_course,
                                    id_isCourse = string.IsNullOrWhiteSpace(ThuocHocPhan) ? null : CheckIsHocPhan.id,
                                    credits = int.Parse(tinchi),
                                    totalPractice = string.IsNullOrWhiteSpace(tongThucHanh) ? 0 : int.Parse(tongThucHanh),
                                    totalTheory = string.IsNullOrWhiteSpace(tongLyThuyet) ? 0 : int.Parse(tongLyThuyet),
                                    id_program = IdProgram,
                                    id_key_year_semester = string.IsNullOrWhiteSpace(ThuocKhoaHoc) ? null : CheckKhoaHoc.id_key_year_semester,
                                    id_semester = string.IsNullOrWhiteSpace(ThuocHocKy) ? null : CheckHocKy.id_semester,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };
                                db.Courses.Add(check_mh);
                            }
                            else
                            {
                                check_mh.id_gr_course = string.IsNullOrWhiteSpace(nhom_mh) ? null : CheckNhomMH.id_gr_course;
                                check_mh.id_isCourse = string.IsNullOrWhiteSpace(ThuocHocPhan) ? null : CheckIsHocPhan.id;
                                check_mh.credits = int.Parse(tinchi);
                                check_mh.totalPractice = string.IsNullOrWhiteSpace(tongThucHanh) ? 0 : int.Parse(tongThucHanh);
                                check_mh.totalTheory = string.IsNullOrWhiteSpace(tongLyThuyet) ? 0 : int.Parse(tongLyThuyet);
                                check_mh.time_up = unixTimestamp;
                            }
                            await db.SaveChangesAsync();
                        }

                        return Ok(new { message = "Import dữ liệu thành công", success = true });
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(new { message = $"Lỗi khi đọc file Excel: {ex.Message}", success = false });
            }
        }

        [HttpPost]
        [Route("export-danh-sach-mon-hoc-thuoc-don-vi")]
        public async Task<IActionResult> ExportCourse([FromBody] CourseDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = db.Courses.AsNoTracking().Where(x => x.id_program == items.id_program);

            if (items.id_gr_course > 0)
                query = query.Where(x => x.id_gr_course == items.id_gr_course);
            if (items.id_isCourse > 0)
                query = query.Where(x => x.id_isCourse == items.id_isCourse);
            if (items.id_key_year_semester > 0)
                query = query.Where(x => x.id_key_year_semester == items.id_key_year_semester);
            if (items.id_semester > 0)
                query = query.Where(x => x.id_semester == items.id_semester);

            var data = await query
                .OrderByDescending(x => x.id_course)
                .Select(x => new
                {
                    x.code_course,
                    x.name_course,
                    name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                    x.credits,
                    x.totalTheory,
                    x.totalPractice,
                    name_program = x.id_programNavigation.name_program,
                    name_is_course = x.id_isCourseNavigation.name,
                    name_semester = x.id_semesterNavigation.code_semester + " - " + x.id_semesterNavigation.name_semester,
                    name_key_year = x.id_key_year_semesterNavigation.code_key_year_semester + " - " + x.id_key_year_semesterNavigation.name_key_year_semester,
                    x.time_cre,
                    x.time_up
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã môn học","Tên môn học","Nhóm học phần",
                    "Số tín chỉ","Lý thuyết","Thực hành","Thuộc CTĐT",
                    "Loại học phần","Học kỳ","Khóa - Năm","Ngày tạo","Cập nhật"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.code_course;
                ws.Cells[row, 3].Value = item.name_course;
                ws.Cells[row, 4].Value = item.name_gr_course;
                ws.Cells[row, 5].Value = item.credits;
                ws.Cells[row, 6].Value = item.totalTheory;
                ws.Cells[row, 7].Value = item.totalPractice;
                ws.Cells[row, 8].Value = item.name_program;
                ws.Cells[row, 9].Value = item.name_is_course;
                ws.Cells[row, 10].Value = item.name_semester;
                ws.Cells[row, 11].Value = item.name_key_year;
                ws.Cells[row, 12].Value = ConvertUnix(item.time_cre);
                ws.Cells[row, 13].Value = ConvertUnix(item.time_up);
                row++;
            }

            var fileBytes = package.GetAsByteArray();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachMonHoc_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }

        private string ConvertUnix(int? unix)
        {
            if (unix == null || unix <= 0) return "";
            return DateTimeOffset.FromUnixTimeSeconds(unix.Value)
                                 .ToLocalTime()
                                 .ToString("dd/MM/yyyy");
        }


        [HttpPost]
        [Route("export-danh-sach-mon-hoc-chua-co-de-cuong")]
        public async Task<IActionResult> ExportThongKe([FromBody] CourseDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = db.Courses.AsNoTracking().Where(x => x.id_program == items.id_program);
            if (items.id_gr_course > 0)
                query = query.Where(x => x.id_gr_course == items.id_gr_course);
            if (items.id_isCourse > 0)
                query = query.Where(x => x.id_isCourse == items.id_isCourse);
            if (items.id_key_year_semester > 0)
                query = query.Where(x => x.id_key_year_semester == items.id_key_year_semester);
            if (items.id_semester > 0)
                query = query.Where(x => x.id_semester == items.id_semester);

            var data = await query
                .OrderByDescending(x => x.id_course)
                .Select(x => new
                {
                    x.code_course,
                    x.name_course,
                    name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                    name_program = x.id_programNavigation.name_program,
                    name_is_course = x.id_isCourseNavigation.name,
                    name_semester = x.id_semesterNavigation.code_semester + " - " + x.id_semesterNavigation.name_semester,
                    name_key_year = x.id_key_year_semesterNavigation.code_key_year_semester + " - " + x.id_key_year_semesterNavigation.name_key_year_semester,
                    status = db.Syllabi.Any(g => g.id_teacherbysubjectNavigation.id_course == x.id_course && g.id_status == 4)
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã môn học","Tên môn học","Nhóm học phần","Thuộc CTĐT",
                    "Loại học phần","Học kỳ","Khóa - Năm","Trạng thái tồn tại đề cương"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;
            foreach (var item in data)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.code_course;
                ws.Cells[row, 3].Value = item.name_course;
                ws.Cells[row, 4].Value = item.name_gr_course;
                ws.Cells[row, 5].Value = item.name_program;
                ws.Cells[row, 6].Value = item.name_is_course;
                ws.Cells[row, 7].Value = item.name_semester;
                ws.Cells[row, 8].Value = item.name_key_year;
                ws.Cells[row, 9].Value = item.status;
                row++;
            }

            var fileBytes = package.GetAsByteArray();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DanhSachMonHoc_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            );
        }
    }
}
