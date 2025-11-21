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
    [Route("api/donvi/course-objectives")]
    [ApiController]
    public class COsDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();

        public COsDVAPI(QLDCContext _db)
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
        [Route("load-du-lieu-muc-tieu-hoc-phan")]
        public async Task<IActionResult> LoadData([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var totalRecords = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .CountAsync();
            var GetItems = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id,
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity,
                    x.time_up,
                    x.time_cre
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
        [Route("them-moi-muc-tieu-hoc-phan")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();

            if (string.IsNullOrEmpty(items.name_CO))
                return Ok(new { message = "Không được bỏ trống trường Tên Mục tiêu học phần", success = false });
            if (string.IsNullOrEmpty(items.describe_CO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung Mục tiêu học phần", success = false });
            var CheckCO = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0) && x.name_CO.ToLower().Trim() == items.name_CO.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckCO != null)
                return Ok(new { message = "Mục tiêu học phần này đã tồn tại, vui lòng kiểm tra lại dữ liệu", success = false });

            var new_record = new CourseObjective
            {
                name_CO = items.name_CO,
                describe_CO = items.describe_CO,
                typeOfCapacity = items.typeOfCapacity,
                id_faculty = GetFaculty.FirstOrDefault(),
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CourseObjectives.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-muc-tieu-hoc-phan")]
        public async Task<IActionResult> InfoCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var checkInfo = await db.CourseObjectives
                .Where(x => x.id == items.id)
                .Select(x => new
                {
                    x.id,
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity,
                })
                .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Mục tiêu học phần", success = false });
            return Ok(checkInfo);
        }
        [HttpPost]
        [Route("update-muc-tieu-hoc-phan")]
        public async Task<IActionResult> UpdateCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();

            if (string.IsNullOrEmpty(items.name_CO))
                return Ok(new { message = "Không được bỏ trống trường Tên Mục tiêu học phần", success = false });

            if (string.IsNullOrEmpty(items.describe_CO))
                return Ok(new { message = "Không được bỏ trống trường Nội dung Mục tiêu học phần", success = false });

            var checkCO = await db.CourseObjectives
                .FirstOrDefaultAsync(x => x.id == items.id);

            if (checkCO == null)
                return Ok(new { message = "Không tìm thấy thông tin Mục tiêu học phần", success = false });

            checkCO.name_CO = items.name_CO.Trim();
            checkCO.describe_CO = items.describe_CO.Trim();
            checkCO.typeOfCapacity = items.typeOfCapacity?.Trim();
            checkCO.time_up = unixTimestamp;

            await db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin Mục tiêu học phần thành công", success = true });
        }

        [HttpPost]
        [Route("xoa-du-lieu-muc-tieu-hoc-phan")]
        public async Task<IActionResult> DeleteCO([FromBody] CODTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var CheckCLO_CO = await db.CLO_CO_Mappings.FirstOrDefaultAsync(x => x.id_CO == items.id);
            if (CheckCLO_CO != null)
                return Ok(new { message = "Mục tiêu học phần này đang tồn tại trong đề cương, không thể xóa", success = false });
            var CheckCO = await db.CourseObjectives.FirstOrDefaultAsync(x => x.id == items.id);
            if (CheckCO == null)
                return Ok(new { message = "Không tìm thầy thông tin Mục tiêu học phần", success = false });
            db.CourseObjectives.Remove(CheckCO);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        [HttpPost("upload-excel-danh-sach-muc-tieu-hoc-phan")]
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
                            var ten_co = worksheet.Cells[row, 2].Text?.Trim();
                            var noi_dung_co = worksheet.Cells[row, 3].Text?.Trim();
                            var loai_muc_tieu = worksheet.Cells[row, 4].Text?.Trim();
                            var check_co = await db.CourseObjectives
                                .FirstOrDefaultAsync(x =>
                                    x.name_CO.ToLower().Trim() == ten_co.ToLower() &&
                                    x.id_faculty == GetFaculty.FirstOrDefault()
                                    );
                            if (check_co == null)
                            {
                                check_co = new CourseObjective
                                {
                                    name_CO = string.IsNullOrWhiteSpace(ten_co) ? null : ten_co.ToUpper(),
                                    describe_CO = string.IsNullOrWhiteSpace(noi_dung_co) ? null : noi_dung_co,
                                    typeOfCapacity = string.IsNullOrWhiteSpace(loai_muc_tieu) ? null : loai_muc_tieu,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };

                                db.CourseObjectives.Add(check_co);

                            }
                            else
                            {
                                check_co.describe_CO = string.IsNullOrWhiteSpace(noi_dung_co) ? null : noi_dung_co;
                                check_co.typeOfCapacity = string.IsNullOrWhiteSpace(loai_muc_tieu) ? null : loai_muc_tieu;
                                check_co.time_up = unixTimestamp;
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
        [Route("export-danh-sach-muc-tieu-hoc-phan")]
        public async Task<IActionResult> Export([FromBody] SemesterDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            GetFaculty = await GetUserPermissionFaculties();;
            var GetItems = await db.CourseObjectives
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id)
                .Select(x => new
                {
                    x.id,
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity,
                    x.time_up,
                    x.time_cre
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Exports");

            string[] headers = {
                    "STT","Tên mục tiêu học phần","Mô tả mục tiêu học phần", "Loại mục tiêu học phần","Ngày tạo","Cập nhật lần cuối"
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
                ws.Cells[row, 2].Value = item.name_CO;
                ws.Cells[row, 3].Value = item.describe_CO;
                ws.Cells[row, 4].Value = item.typeOfCapacity;
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
