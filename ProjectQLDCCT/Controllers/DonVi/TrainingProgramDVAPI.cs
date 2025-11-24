using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/program")]
    [ApiController]
    public class TrainingProgramDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public TrainingProgramDVAPI(QLDCContext _db)
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
        [Route("loads-select-don-vi")]
        public async Task<IActionResult> LoadDonViByCTDT()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var GetDV = await db.Faculties
                .Where(x => GetFaculty.Contains(x.id_faculty))
                .Select(x => new
                {
                    value = x.id_faculty,
                    name = x.name_faculty
                })
                .ToListAsync();
            return Ok(GetDV);
        }
        [HttpPost]
        [Route("loads-ctdt-thuoc-don-vi")]
        public async Task<IActionResult> LoadData([FromBody] TrainingProgramDTOs items)
        {
            var query = db.TrainingPrograms.AsQueryable();

            if (items.id_faculty > 0)
            {
                query = query.Where(x => x.id_faculty == items.id_faculty); ;
            }
            var totalRecords = await query.CountAsync();
            var _query = await query
                .OrderByDescending(x => x.id_program)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_program,
                    x.code_program,
                    x.name_program,
                    x.time_up,
                    x.time_cre,
                    x.id_facultyNavigation.name_faculty
                }).ToListAsync();
            return Ok(new
            {
                success = true,
                data = _query,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-ctdt")]
        public async Task<IActionResult> ThemMoiCTDT([FromBody] TrainingProgramDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_program))
                return Ok(new { message = "Không được bỏ trống Mã CTĐT", success = false });
            if (string.IsNullOrEmpty(items.name_program))
                return Ok(new { message = "Không được bỏ trống Tên CTĐT", success = false });
            var checkCodeProgram = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.code_program.ToLower().Trim() == items.code_program.ToLower().Trim());
            var checkNameProgram = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.name_program.ToLower().Trim() == items.name_program.ToLower().Trim());
            if (checkCodeProgram != null)
            {
                return Ok(new { message = "Mã CTĐT này đã tồn tại, vui lòng kiểm tra lại", success = false });
            }

            if (checkNameProgram != null)
            {
                return Ok(new { message = "Tên CTĐT này đã tồn tại, vui lòng kiểm tra lại", success = false });
            }
            var new_record = new TrainingProgram
            {
                code_program = items.code_program,
                name_program = items.name_program,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                id_faculty = items.id_faculty
            };
            db.TrainingPrograms.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("get-thong-tin-ctdt")]
        public async Task<IActionResult> InfoCTDT([FromBody] TrainingProgramDTOs items)
        {
            var GetInfo = await db.TrainingPrograms
                .Where(x => x.id_program == items.id_program)
                .Select(x => new
                {
                    x.code_program,
                    x.name_program,
                    x.id_faculty,
                    x.id_program
                })
                .FirstOrDefaultAsync();
            return Ok(GetInfo);
        }
        [HttpPost]
        [Route("cap-nhat-thong-tin-ctdt")]
        public async Task<IActionResult> UpdateCTDT([FromBody] TrainingProgramDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_program))
                return Ok(new { message = "Không được bỏ trống Mã CTĐT", success = false });
            if (string.IsNullOrEmpty(items.name_program))
                return Ok(new { message = "Không được bỏ trống Tên CTĐT", success = false });

            var CheckItems = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.id_program == items.id_program);
            CheckItems.id_faculty = items.id_faculty;
            CheckItems.code_program = items.code_program;
            CheckItems.name_program = items.name_program;
            CheckItems.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-ctdt")]
        public async Task<IActionResult> DeleteCTDT([FromBody] TrainingProgramDTOs items)
        {
            var CheckItems = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.id_program == items.id_program);
            if (CheckItems == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin CTĐT", success = false });
            }
            db.TrainingPrograms.Remove(CheckItems);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-chuong-trinh-dao-tao")]
        public async Task<IActionResult> UploadExcelMonHoc(IFormFile file)
        {
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
                            var ma_ctdt = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_ctdt = worksheet.Cells[row, 3].Text?.Trim();
                            var check_ctdt = await db.TrainingPrograms
                                .FirstOrDefaultAsync(x =>
                                   (x.code_program.ToLower().Trim() == ma_ctdt.ToLower().Trim() && x.name_program.ToLower().Trim() == ten_ctdt.ToLower().Trim()));

                            if (check_ctdt == null)
                            {
                                check_ctdt = new TrainingProgram
                                {
                                    id_faculty = IdProgram,
                                    code_program = string.IsNullOrWhiteSpace(ma_ctdt) ? null : ma_ctdt.ToUpper(),
                                    name_program = string.IsNullOrWhiteSpace(ten_ctdt) ? null : ten_ctdt,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };
                                db.TrainingPrograms.Add(check_ctdt);
                            }
                            else
                            {
                                check_ctdt.time_up = unixTimestamp;
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
        [Route("export-danh-sach-ctdt-thuoc-don-vi")]
        public async Task<IActionResult> ExportCourse([FromBody] TrainingProgramDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = db.TrainingPrograms.AsQueryable();

            if (items.id_faculty > 0)
            {
                query = query.Where(x => x.id_faculty == items.id_faculty); ;
            }
            var totalRecords = await query.CountAsync();
            var _query = await query
                .OrderByDescending(x => x.id_program)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_program,
                    x.code_program,
                    x.name_program,
                    x.time_up,
                    x.time_cre,
                    x.id_facultyNavigation.name_faculty
                }).ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã chương trình đào tạo","Tên chương trình đào tạo","Thuộc đơn vị",
                    "Ngày tạo","Cập nhật"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in _query)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.code_program;
                ws.Cells[row, 3].Value = item.name_program;
                ws.Cells[row, 4].Value = item.name_faculty;
                ws.Cells[row, 5].Value = ConvertUnix(item.time_cre);
                ws.Cells[row, 6].Value = ConvertUnix(item.time_up);
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
    }
}
