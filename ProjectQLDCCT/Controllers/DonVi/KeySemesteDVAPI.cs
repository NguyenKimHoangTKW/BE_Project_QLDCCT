using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Authorize(Policy = "DonVi")]
    [Route("api/donvi/key-semester")]
    [ApiController]
    public class KeySemesteDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public KeySemesteDVAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("loads-danh-sach-khoa-hoc")]
        public async Task<IActionResult> LoadListKeySemester([FromBody] KeySemesterDTOs items)
        {
           
            var query = db.KeyYearSemesters
                .Where(x => x.id_faculty == items.id_faculty).AsQueryable();
            if (!string.IsNullOrEmpty(items.searchTerm))
            {
                string keyword = items.searchTerm.ToLower();
                query = query.Where(x =>
                x.code_key_year_semester.ToLower().Contains(keyword) ||
                x.name_key_year_semester.ToLower().Contains(keyword));
            }
            var totalRecords = await query
               .Where(x => x.id_faculty == items.id_faculty).CountAsync();
            var KeySemesterList = await query
                .Where(x => x.id_faculty == items.id_faculty)
                .OrderByDescending(x => x.id_key_year_semester)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_key_year_semester,
                    x.code_key_year_semester,
                    x.name_key_year_semester,
                    x.time_cre,
                    x.time_up,
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = KeySemesterList,
                currentPage = items.Page,
                pageSize = items.PageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }

        [HttpPost]
        [Route("them-moi-khoa-hoc")]
        public async Task<IActionResult> ThemMoiKeySemester([FromBody] KeySemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Tên khóa học", success = false });
            if (string.IsNullOrEmpty(items.code_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã khóa học", success = false });
            var CheckKeySemester = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.name_key_year_semester.ToLower().Trim() == items.name_key_year_semester.ToLower().Trim());
            if (CheckKeySemester != null)
            {
                return Ok(new { message = "Khóa học này đã tồn tại, vui lòng kiểm tra lại", success = false });
            }

            var new_record = new KeyYearSemester
            {
                id_faculty = items.id_faculty,
                code_key_year_semester = items.code_key_year_semester,
                name_key_year_semester = items.name_key_year_semester,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.KeyYearSemesters.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-khoa-hoc")]
        public async Task<IActionResult> InfoKeySemester([FromBody] KeySemesterDTOs items)
        {
            var checkKeySemester = await db.KeyYearSemesters
                .Where(x => x.id_key_year_semester == items.id_key_year_semester)
                .Select(x => new
                {
                    x.id_key_year_semester,
                    x.code_key_year_semester,
                    x.name_key_year_semester,
                })
                .FirstOrDefaultAsync();
            if (checkKeySemester == null)
                return Ok(new { message = "Không tìm thấy thông tin khóa học", success = false });

            return Ok(new { data = checkKeySemester, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-khoa-hoc")]
        public async Task<IActionResult> UpdateKeySemester([FromBody] KeySemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Tên khóa học", success = false });
            if (string.IsNullOrEmpty(items.code_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã khóa học", success = false });
            var CheckKeySemester = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);
            if (CheckKeySemester == null)
                return Ok(new { message = "Không tìm thấy thông tin Khóa học", success = false });
            CheckKeySemester.code_key_year_semester = items.code_key_year_semester;
            CheckKeySemester.name_key_year_semester = items.name_key_year_semester;
            CheckKeySemester.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-khoa-hoc")]
        public async Task<IActionResult> DeleteKeySemester([FromBody] KeySemesterDTOs items)
        {
            var CheckCourse = await db.CourseByKeys.FirstOrDefaultAsync(x => x.id_key_semester == items.id_key_year_semester);
            if (CheckCourse != null)
                return Ok(new { message = "Khóa học này đang tồn tại dữ liệu trong Môn học, không thể xóa", success = false });

            var CheckKey = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);
            if (CheckKey == null)
                return Ok(new { message = "Không tìm thấy thông tin Khóa học", success = false });

            db.KeyYearSemesters.Remove(CheckKey);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-danh-sach-khoa-hoc")]
        public async Task<IActionResult> UploadExcelMonHoc(IFormFile file)
        {

            if (file == null || file.Length == 0)
                return Ok(new { message = "Vui lòng chọn file Excel.", success = false });

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return Ok(new { message = "Chỉ hỗ trợ upload file Excel.", success = false });
            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                var ID = Request.Form["id_faculty"];
                int IdFaculty = int.Parse(ID);
                if (IdFaculty == 0)
                {
                    return Ok(new { message = "Vui lòng chọn Đơn vị cố định để Import", success = false });
                }
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
                            var ma_kh = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_kh = worksheet.Cells[row, 3].Text?.Trim();

                            var check_kh = await db.KeyYearSemesters
                                .FirstOrDefaultAsync(x =>
                                    x.code_key_year_semester.ToLower().Trim() == ma_kh.ToLower() &&
                                    x.name_key_year_semester.ToLower().Trim() == ten_kh.ToLower() &&
                                    x.id_faculty == IdFaculty
                                    );
                            if (check_kh == null)
                            {
                                check_kh = new KeyYearSemester
                                {
                                    code_key_year_semester = string.IsNullOrWhiteSpace(ma_kh) ? null : ma_kh.ToUpper(),
                                    name_key_year_semester = string.IsNullOrWhiteSpace(ten_kh) ? null : ten_kh,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };

                                db.KeyYearSemesters.Add(check_kh);

                            }
                            else
                            {
                                check_kh.time_up = unixTimestamp;
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
        [Route("export-danh-sach-khoa-hoc-thuoc-don-vi")]
        public async Task<IActionResult> Export([FromBody] SemesterDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var KeySemesterList = await db.KeyYearSemesters
                 .Where(x => x.id_faculty == items.id_faculty)
                 .OrderByDescending(x => x.id_key_year_semester)
                 .Select(x => new
                 {
                     x.id_key_year_semester,
                     x.code_key_year_semester,
                     x.name_key_year_semester,
                     x.time_cre,
                     x.time_up,
                 })
                 .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã khóa học","Tên khóa học", "Ngày tạo","Cập nhật lần cuối"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in KeySemesterList)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.code_key_year_semester;
                ws.Cells[row, 3].Value = item.name_key_year_semester;
                ws.Cells[row, 4].Value = ConvertUnix(item.time_cre);
                ws.Cells[row, 5].Value = ConvertUnix(item.time_up);
                row++;
            }

            var fileBytes = package.GetAsByteArray();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Exports.xlsx"
            );
        }

        private string ConvertUnix(int? unix)
        {
            if (unix == null || unix <= 0) return "";
            return DateTimeOffset.FromUnixTimeSeconds(unix.Value)
                                 .ToLocalTime()
                                 .ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
