using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.CTDT
{
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


        [HttpGet]
        [Route("load-select-chuc-nang-course")]
        public async Task<IActionResult> LoadOptionSelectedUpdate()
        {

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
            return Ok(new
            {
                nhom_hoc_phan = LoadListGroupHocPhan,
                is_hoc_phan = LoadIsHocPhan
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
                    x.time_cre,
                    x.time_up,
                    name = x.id_isCourseNavigation.name
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data,
                currentPage =items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
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
            checkCourse.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-mon-hoc")]
        public async Task<IActionResult> DeleteMonHoc([FromBody] CourseDTOs items)
        {
            var checkSyllabus = await db.Syllabi.FirstOrDefaultAsync(x => x.id_course == items.id_course);
            if (checkSyllabus != null)
            {
                return Ok(new { message = "Môn học này đang tồn tại Đề cương, không thể xóa", success = false });
            }
            var CheckCourse = await db.Courses.FirstOrDefaultAsync(x => x.id_course == items.id_course);
            if (CheckCourse == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin Môn học", success = false });
            }

            db.Courses.Remove(CheckCourse);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-danh-sach-mon-hoc")]
        public async Task<IActionResult> UploadExcelMonHoc(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Ok(new { message = "Vui lòng chọn file Excel.", success = false });

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return Ok(new { message = "Chỉ hỗ trợ upload file Excel.", success = false });
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

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
                            var check_mh = await db.Courses
                                .FirstOrDefaultAsync(x =>
                                    x.code_course.ToLower().Trim() == ma_mh.ToLower() &&
                                    x.code_course.ToLower().Trim() == ma_mh.ToLower());

                            if (check_mh == null)
                            {
                                check_mh = new Course
                                {
                                    code_course = string.IsNullOrWhiteSpace(ma_mh) ? null : ma_mh.ToUpper(),
                                    name_course = ten_mh,
                                    id_gr_course = string.IsNullOrWhiteSpace(nhom_mh) ? null : CheckNhomMH.id_gr_course,
                                    id_isCourse = string.IsNullOrWhiteSpace(ThuocHocPhan) ? null : CheckIsHocPhan.id,
                                    credits = int.Parse(tinchi),
                                    totalPractice = string.IsNullOrWhiteSpace(tongThucHanh) ? null : int.Parse(tongThucHanh),
                                    totalTheory = string.IsNullOrWhiteSpace(tongLyThuyet) ? null : int.Parse(tongLyThuyet),
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };
                                db.Courses.Add(check_mh);
                            }
                            else
                            {
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
    }
}
