using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Drawing.Printing;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Authorize(Policy = "DonVi")]
    [Route("api/donvi/semester")]
    [ApiController]
    public class HocKyDVApi : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public HocKyDVApi(QLDCContext _db)
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
        [Route("loads-danh-sach-hoc-ky")]
        public async Task<IActionResult> LoadsHocKy([FromBody] SemesterDTOs? items)
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var query = db.Semesters
                .Where(x => x.id_faculty == items.id_faculty).AsQueryable();

            if (!string.IsNullOrEmpty(items.searchTerm))
            {
                string keyword = items.searchTerm.ToLower();
                query = query.Where(x =>
                x.code_semester.ToLower().Contains(keyword) ||
                x.name_semester.ToLower().Contains(keyword));
            }
            var totalRecords =await query
                .Where(x => x.id_faculty == items.id_faculty)
                .CountAsync();

            var hocKyList =await query
                .Where(x => x.id_faculty == items.id_faculty)
                .OrderByDescending(x => x.id_semester)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_semester,
                    x.code_semester,
                    x.name_semester,
                    x.tim_cre,
                    x.time_up
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = hocKyList,
                currentPage = items.Page,
                pageSize = items.PageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-hoc-kyf")]
        public async Task<IActionResult> ThemsMoiHocKy([FromBody] SemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_semester))
                return Ok(new { message = "Không được bỏ trống trường tên Học kỳ", success = false });
            if (string.IsNullOrEmpty(items.code_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã Học kỳ", success = false });
            var checkHocKyRecord = await db.Semesters.Where(x => x.name_semester.ToLower().Trim() == items.name_semester.ToLower().Trim()).FirstOrDefaultAsync();
            if (checkHocKyRecord != null)
                return Ok(new { message = "Học kỳ này đã tồn tại trong CSDL, vui lòng kiểm tra lại", success = false });
            var new_record = new Semester
            {
                name_semester = items.name_semester,
                id_faculty = items.id_faculty,
                code_semester = items.code_semester,
                tim_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.Semesters.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới học kỳ thành công", success = true });
        }
        [HttpPost]
        [Route("info-hoc-ky")]
        public async Task<IActionResult> InfoHocKys([FromBody] SemesterDTOs items)
        {
            if (items.id_semester == null | items.id_semester == 0)
                return Ok(new { message = "Không tìm thấy thông tin học kỳ", success = false });
            var CheckHocKys = await db.Semesters
                .Where(x => x.id_semester == items.id_semester)
                .Select(x => new
                {
                    x.id_faculty,
                    x.name_semester,
                    x.code_semester,
                    x.id_semester
                })
                .FirstOrDefaultAsync();
            return Ok(new { data = CheckHocKys, success = true });
        }
        [HttpPost]
        [Route("update-hoc-kys")]
        public async Task<IActionResult> UpdateHocKys([FromBody] SemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_semester))
                return Ok(new { message = "Không được bỏ trống trường tên Học kỳ", success = false });
            var checkHocKyRecord = await db.Semesters.Where(x => x.id_semester == items.id_semester).FirstOrDefaultAsync();

            checkHocKyRecord.name_semester = items.name_semester;
            checkHocKyRecord.code_semester = items.code_semester;
            checkHocKyRecord.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("delete-hoc-kys")]
        public async Task<IActionResult> DeleteHocKys([FromBody] SemesterDTOs items)
        {
            var CheckMonHoc = await db.CourseByKeys.FirstOrDefaultAsync(x => x.id_semester == items.id_semester);
            if (CheckMonHoc != null)
                return Ok(new { message = "Học kỳ này đang tồn tại môn học của chương trình, không thể xóa", success = false });
            var CheckHockys = await db.Semesters.FirstOrDefaultAsync(x => x.id_semester == items.id_semester);
            if (CheckHockys == null)
                return Ok(new { message = "Không tìm thấy thông tin Học kỳ", success = false });

            db.Semesters.Remove(CheckHockys);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-danh-sach-hoc-ky")]
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
                            var ma_hk = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_hk = worksheet.Cells[row, 3].Text?.Trim();

                            var check_hk = await db.Semesters
                                .FirstOrDefaultAsync(x =>
                                    x.code_semester.ToLower().Trim() == ma_hk.ToLower() &&
                                    x.name_semester.ToLower().Trim() == ten_hk.ToLower() &&
                                    x.id_faculty == IdFaculty
                                    );
                            if (check_hk == null)
                            {
                                check_hk = new Semester
                                {
                                    code_semester = string.IsNullOrWhiteSpace(ma_hk) ? null : ma_hk.ToUpper(),
                                    name_semester = string.IsNullOrWhiteSpace(ten_hk) ? null : ten_hk,
                                    tim_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };

                                db.Semesters.Add(check_hk);

                            }
                            else
                            {
                                check_hk.time_up = unixTimestamp;
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
        [Route("export-danh-sach-hoc-ky-thuoc-don-vi")]
        public async Task<IActionResult> Export([FromBody] SemesterDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var hocKyList = await db.Semesters
               .Where(x => x.id_faculty == items.id_faculty)
               .OrderByDescending(x => x.id_semester)
               .Select(x => new
               {
                   x.id_semester,
                   x.code_semester,
                   x.name_semester,
                   x.tim_cre,
                   x.time_up
               })
               .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã học kỳ","Tên học kỳ", "Ngày tạo","Cập nhật lần cuối"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in hocKyList)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.code_semester;
                ws.Cells[row, 3].Value = item.name_semester;
                ws.Cells[row, 4].Value = ConvertUnix(item.tim_cre);
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
