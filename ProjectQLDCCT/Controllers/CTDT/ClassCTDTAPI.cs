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
    [Route("api/ctdt/class")]
    [ApiController]
    public class ClassCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public ClassCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpPost("list-class")]
        public async Task<IActionResult> LoadListClass([FromBody] ClassDTOs items)
        {
            var LoadClass = await db.Classes
                .Where(x => x.id_program == items.id_program)
                .OrderByDescending(x => x.id_class)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_class,
                    x.name_class,
                    x.time_up,
                    x.tim_cre
                })
                .ToListAsync();
            var totalRecords = await db.Classes
              .Where(x => x.id_program == items.id_program).CountAsync();
            return Ok(new
            {
                success = true,
                LoadClass,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }

        [HttpPost("them-moi-lop")]
        public async Task<IActionResult> AddNew([FromBody] ClassDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_class))
                return Ok(new { message = "Không được bỏ trống tên lớp", success = false });

            var CheckLop = await db.Classes.Where(x => x.id_program == items.id_program && x.name_class.ToLower().Trim() == items.name_class.ToLower().Trim()).FirstOrDefaultAsync();
            if (CheckLop != null)
                return Ok(new { message = "Tên lớp này đã tồn tại trong chương trình đào tạo, vui lòng kiểm tra lại", success = false });

            var new_record = new Class
            {
                name_class = items.name_class,
                id_program = items.id_program,
                time_up = unixTimestamp,
                tim_cre = unixTimestamp
            };
            db.Classes.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost("info-lop")]
        public async Task<IActionResult> InfoClass([FromBody] ClassDTOs items)
        {
            var CheckClass = await db.Classes
                .Where(x => x.id_class == items.id_class)
                .Select(x => new
                {
                    x.id_class,
                    x.name_class,
                })
                .FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin Lớp, vui lòng kiểm tra lại", success = false });
            return Ok(new { data = CheckClass, success = true });
        }
        [HttpPost("update-lop")]
        public async Task<IActionResult> UpdateClass([FromBody] ClassDTOs items)
        {
            var CheckClass = await db.Classes.Where(x => x.id_class == items.id_class).FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin Lớp, vui lòng kiểm tra lại", success = false });
            CheckClass.name_class = items.name_class;
            CheckClass.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost("delete-lop")]
        public async Task<IActionResult> DeleteClass([FromBody] ClassDTOs items)
        {
            var CheckClass = await db.Classes.Where(x => x.id_class == items.id_class).FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin Lớp, vui lòng kiểm tra lại", success = false });
            db.Classes.Remove(CheckClass);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        [HttpPost("upload-excel-danh-sach-lop")]
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
                var checkCtdt = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.id_program == IdProgram);
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
                            var ten_class = worksheet.Cells[row, 2].Text?.Trim();
                            var check_lop = await db.Classes
                                .FirstOrDefaultAsync(x =>
                                    x.name_class.ToLower().Trim() == ten_class.ToLower().Trim() &&
                                    x.id_program == IdProgram
                                    );
                            if (check_lop == null)
                            {
                                check_lop = new Class
                                {
                                    name_class = ten_class.ToUpper(),
                                    id_program = IdProgram,
                                    tim_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };
                                db.Classes.Add(check_lop);
                            }
                            else
                            {
                                check_lop.name_class = string.IsNullOrWhiteSpace(ten_class) ? null : ten_class.ToUpper() ;
                                check_lop.time_up = unixTimestamp;
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
        [Route("export-danh-sach-lop")]
        public async Task<IActionResult> ExportCourse([FromBody] CourseDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var LoadClass = await db.Classes
                .Where(x => x.id_program == items.id_program)
                .OrderByDescending(x => x.id_class)
                .Select(x => new
                {
                    x.name_class,
                    x.id_programNavigation.name_program,
                    x.time_up,
                    x.tim_cre
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachLop");

            string[] headers = {
                    "STT","Tên lớp","Thuộc CTĐT","Ngày tạo","Cập nhật"
                };

            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[1, i + 1].Value = headers[i];
                ws.Column(i + 1).Width = 20;
            }

            int row = 2;
            int index = 1;

            foreach (var item in LoadClass)
            {
                ws.Cells[row, 1].Value = index++;
                ws.Cells[row, 2].Value = item.name_class;
                ws.Cells[row, 3].Value = item.name_program;
                ws.Cells[row, 4].Value = ConvertUnix(item.tim_cre);
                ws.Cells[row, 5].Value = ConvertUnix(item.time_up);
                row++;
            }

            var fileBytes = package.GetAsByteArray();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Export.xlsx"
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
