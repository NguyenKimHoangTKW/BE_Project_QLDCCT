using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Globalization;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Authorize(Policy = "Admin")]
    [Route("api/admin/civilservants")]
    [ApiController]
    public class CivilServantsApiAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CivilServantsApiAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpGet]
        [Route("load-don-vi-by-civiservants")]
        public async Task<IActionResult> LoadOption()
        {
            var GetListFaculty = await db.Faculties
                .Select(x => new
                {
                    x.id_faculty,
                    x.name_faculty
                })
                .ToListAsync();
            return Ok(GetListFaculty);
        }
        [HttpPost]
        [Route("load-ctdt-by-don-vi-civilservants")]
        public async Task<IActionResult> LoadOptionCTDT([FromBody] TrainingProgramDTOs items)
        {
            var GetList = await db.TrainingPrograms
                .Where(x => x.id_faculty == items.id_faculty)
                .Select(x => new
                {
                    x.id_program,
                    x.name_program
                })
                .ToListAsync();
            return Ok(GetList);
        }
        [HttpPost]
        [Route("loads-danh-sach-can-bo-vien-chuc")]
        public async Task<IActionResult> LoadCBVC([FromBody] CivilServantsDTOs items)
        {
            
            var query = db.CivilServants.AsQueryable();
            if (items.id_program > 0)
            {
                query = query.Where(x => x.id_program == items.id_program);
            }

            if(items.id_faculty > 0)
            {
                query = query.Where(x => x.id_programNavigation.id_faculty == items.id_faculty);
            }
            var totalRecords = await query
                .CountAsync();
            var GetItems = await query
                .OrderByDescending(x => x.id_civilSer)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    x.id_programNavigation.name_program,
                    x.id_programNavigation.id_facultyNavigation.name_faculty,
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
        [Route("them-moi-can-bo-vien-chuc")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });
            var CheckNameCV = await db.CivilServants
                .Where(x => x.id_programNavigation.id_faculty == items.id_faculty && x.code_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim() && x.fullname_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckNameCV != null)
                return Ok(new { message = "Cán bộ viên chức này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var CheckEmailCV = await db.CivilServants
                .Where(x => x.id_programNavigation.id_faculty == items.id_faculty && x.email.ToLower().Trim() == items.email.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckEmailCV != null)
                return Ok(new { message = "Email này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var new_record = new CivilServant
            {
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                code_civilSer = items.code_civilSer,
                birthday = items.birthday,
                id_program = items.id_program,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CivilServants.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-can-bo-vien-chuc")]
        public async Task<IActionResult> InfoCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
                .Where(x => x.id_civilSer == items.id_civilSer)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.email,
                    x.code_civilSer,
                    x.birthday,
                    x.fullname_civilSer,
                    x.id_program
                })
                .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            return Ok(new { data = checkInfo, success = true });
        }
        [HttpPost]
        [Route("update-can-bo-vien-chuc")]
        public async Task<IActionResult> UpdateCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });

            var checkInfo = await db.CivilServants
              .Where(x => x.id_civilSer == items.id_civilSer)
              .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            checkInfo.fullname_civilSer = items.fullname_civilSer;
            checkInfo.code_civilSer = items.code_civilSer;
            checkInfo.email = items.email;
            checkInfo.birthday = items.birthday;
            checkInfo.id_program = items.id_program;
            checkInfo.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-can-bo-vien-chuc")]
        public async Task<IActionResult> DeleteCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
            .Where(x => x.id_civilSer == items.id_civilSer)
            .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            db.CivilServants.Remove(checkInfo);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost("upload-excel-danh-sach-giang-vien")]
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
                if (IdProgram == 0)
                {
                    return Ok(new { message = "Vui lòng chọn Chương trình đào tạo cố định để Import", success = false });
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
                            var ma_gv = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_gv = worksheet.Cells[row, 3].Text?.Trim();
                            var email = worksheet.Cells[row, 4].Text?.Trim();
                            var ngaysinh = worksheet.Cells[row, 5].Text?.Trim();

                            var check_gv = await db.CivilServants
                                .FirstOrDefaultAsync(x =>
                                    x.code_civilSer.ToLower().Trim() == ma_gv.ToLower() &&
                                    x.fullname_civilSer.ToLower().Trim() == ten_gv.ToLower() &&
                                    x.id_program == IdProgram
                                    );
                            if (check_gv == null)
                            {
                                check_gv = new CivilServant
                                {
                                    code_civilSer = string.IsNullOrWhiteSpace(ma_gv) ? null : ma_gv.ToUpper(),
                                    fullname_civilSer = string.IsNullOrWhiteSpace(ten_gv) ? null : ten_gv,
                                    email = string.IsNullOrWhiteSpace(email) ? null : email,
                                    birthday = ParseDateOnly(ngaysinh),
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };

                                db.CivilServants.Add(check_gv);

                            }
                            else
                            {
                                check_gv.email = string.IsNullOrWhiteSpace(email) ? null : email;
                                check_gv.time_up = unixTimestamp;
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
        private DateOnly? ParseDateOnly(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            DateTime dt;

            var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };

            if (DateTime.TryParseExact(input.Trim(), formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out dt))
            {
                return DateOnly.FromDateTime(dt);
            }

            return null;
        }


        [HttpPost]
        [Route("export-danh-sach-giang-vien-thuoc-don-vi")]
        public async Task<IActionResult> Export([FromBody] CivilServantsDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = db.CivilServants.AsQueryable();
            if (items.id_program > 0)
            {
                query = query.Where(x => x.id_program == items.id_program);
            }

            if (items.id_faculty > 0)
            {
                query = query.Where(x => x.id_programNavigation.id_faculty == items.id_faculty);
            }
            var GetItems = await query
                .OrderByDescending(x => x.id_civilSer)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    x.id_programNavigation.name_program,
                    x.id_programNavigation.id_facultyNavigation.name_faculty,
                    x.time_up,
                    x.time_cre
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachMonHoc");

            string[] headers = {
                    "STT","Mã giảng viên","Tên giảng viên","Email",
                    "Ngày sinh","Thuộc CTĐT","Thuộc Đơn vị","Ngày tạo","Cập nhật lần cuối"
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
                ws.Cells[row, 2].Value = item.code_civilSer;
                ws.Cells[row, 3].Value = item.fullname_civilSer;
                ws.Cells[row, 4].Value = item.email;
                ws.Cells[row, 5].Value = item.birthday.HasValue ? item.birthday.Value.ToString("dd-MM-yyyy") : "";
                ws.Cells[row, 6].Value = item.name_program;
                ws.Cells[row, 7].Value = item.name_faculty;
                ws.Cells[row, 8].Value = ConvertUnix(item.time_cre);
                ws.Cells[row, 9].Value = ConvertUnix(item.time_up);
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
