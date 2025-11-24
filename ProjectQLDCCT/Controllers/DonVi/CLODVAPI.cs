using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    [Route("api/donvi/course-learning-outcome")]
    [ApiController]
    public class CLODVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public CLODVAPI(QLDCContext _db)
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
        [Route("load-du-lieu-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> LoadCLO([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var totalRecords = await db.CourseLearningOutcomes.Where(x => GetFaculty.Contains(x.id_faculty ?? 0)).CountAsync();
            var GetItems = await db.CourseLearningOutcomes
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(totalRecords)
                .Select(x => new
                {
                    x.id,
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level,
                    x.time_cre,
                    x.time_up
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
        [Route("them-moi-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> AddNew([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.name_CLO))
                return Ok(new { message = "Không được bỏ trống trường Tên chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CLO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.bloom_level))
                return Ok(new { message = "Không được bỏ trống trường Mức độ Bloom chuẩn đầu ra học phần", success = false });
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.name_CLO.ToLower().Trim() == items.name_CLO.ToLower().Trim()).FirstOrDefaultAsync();
            if (checkCLO != null)
                return Ok(new { message = "Chuẩn đầu ra học phần này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var new_record = new CourseLearningOutcome
            {
                name_CLO = items.name_CLO,
                describe_CLO = items.describe_CLO,
                id_faculty = GetFaculty.FirstOrDefault(),
                bloom_level = items.bloom_level,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CourseLearningOutcomes.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> InfoCLO([FromBody] CLODTOs items)
        {
            var CheckCLO = await db.CourseLearningOutcomes
                .Where(x => x.id == items.id)
                .Select(x => new
                {
                    x.id,
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level,
                    x.program_id
                })
                .FirstOrDefaultAsync();
            if (CheckCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            return Ok(CheckCLO);
        }
        [HttpPost]
        [Route("cap-nhat-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> UpdateCLO([FromBody] CLODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.name_CLO))
                return Ok(new { message = "Không được bỏ trống trường Tên chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CLO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung chuẩn đầu ra học phần", success = false });
            if (string.IsNullOrEmpty(items.bloom_level))
                return Ok(new { message = "Không được bỏ trống trường Mức độ Bloom chuẩn đầu ra học phần", success = false });
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.id == items.id).FirstOrDefaultAsync();
            if (checkCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            checkCLO.name_CLO = items.name_CLO;
            checkCLO.describe_CLO = items.describe_CLO;
            checkCLO.bloom_level = items.bloom_level;
            checkCLO.program_id = items.program_id;
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> DeleteCLO([FromBody] CLODTOs items)
        {
            var checkCLO = await db.CourseLearningOutcomes.Where(x => x.id == items.id).FirstOrDefaultAsync();
            if (checkCLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học phần", success = false });
            db.CourseLearningOutcomes.Remove(checkCLO);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-danh-sach-chuan-dau-ra-hoc-phan")]
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
                            var ten_clo = worksheet.Cells[row, 2].Text?.Trim();
                            var noi_dung_clo = worksheet.Cells[row, 3].Text?.Trim();
                            var muc_do_bloom = worksheet.Cells[row, 4].Text?.Trim();
                            var check_clo = await db.CourseLearningOutcomes
                                .FirstOrDefaultAsync(x =>
                                    x.name_CLO.ToLower().Trim() == ten_clo.ToLower() &&
                                    x.id_faculty == GetFaculty.FirstOrDefault()
                                    );
                            if (check_clo == null)
                            {
                                check_clo = new CourseLearningOutcome
                                {
                                    name_CLO = string.IsNullOrWhiteSpace(ten_clo) ? null : ten_clo.ToUpper(),
                                    describe_CLO = string.IsNullOrWhiteSpace(noi_dung_clo) ? null : noi_dung_clo,
                                    bloom_level = string.IsNullOrWhiteSpace(muc_do_bloom) ? null : muc_do_bloom,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };

                                db.CourseLearningOutcomes.Add(check_clo);

                            }
                            else
                            {
                                check_clo.describe_CLO = string.IsNullOrWhiteSpace(noi_dung_clo) ? null : noi_dung_clo;
                                check_clo.bloom_level = string.IsNullOrWhiteSpace(muc_do_bloom) ? null : muc_do_bloom;
                                check_clo.time_up = unixTimestamp;
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
        [Route("export-danh-sach-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> Export([FromBody] SemesterDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            GetFaculty = await GetUserPermissionFaculties();
            var GetItems = await db.CourseLearningOutcomes
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Select(x => new
                {
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level,
                    x.time_cre,
                    x.time_up
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Exports");

            string[] headers = {
                    "STT","Tên chuẩn đầu ra học phần","Mô tả chuẩn đầu ra học phần", "Mức độ Bloom","Ngày tạo","Cập nhật lần cuối"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in GetItems)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.name_CLO;
                ws.Cells[row, 3].Value = item.describe_CLO;
                ws.Cells[row, 4].Value = item.bloom_level;
                ws.Cells[row, 5].Value = ConvertUnix(item.time_cre);
                ws.Cells[row, 6].Value = ConvertUnix(item.time_up);
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
