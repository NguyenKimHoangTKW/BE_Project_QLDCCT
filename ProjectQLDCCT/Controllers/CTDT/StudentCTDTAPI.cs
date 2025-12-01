using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/ctdt/student")]
    [ApiController]
    public class StudentCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public StudentCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost("get-list-class-by-program")]
        public async Task<IActionResult> GetListClass([FromBody] ClassDTOs items)
        {
            var GetListClass = await db.Classes
                .Where(x => x.id_program == items.id_program)
                .Select(x => new
                {
                    x.id_class,
                    x.name_class
                })
                .ToListAsync();
            return Ok(GetListClass);
        }
        [HttpPost("list-student")]
        public async Task<IActionResult> LoadListClass([FromBody] StudentDTOs items)
        {
            var query = db.Students.Where(x => x.id_classNavigation.id_program == items.id_program).AsQueryable();
            if (items.id_class > 0)
            {
                query = query.Where(x => x.id_class == items.id_class);
            }
            var totalRecords = await db.Students
              .Where(x => x.id_classNavigation.id_program == items.id_program).CountAsync();
            var LoadClass = await query
                .OrderByDescending(x => x.id_class)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_student,
                    x.code_student,
                    x.name_student,
                    x.id_classNavigation.name_class,
                    x.id_classNavigation.id_programNavigation.name_program,
                    x.time_up,
                    x.tim_cre
                })
                .ToListAsync();

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

        [HttpPost("them-moi-sinh-vien")]
        public async Task<IActionResult> AddNew([FromBody] StudentDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_student))
                return Ok(new { message = "Không được bỏ trống tên sinh viên", success = false });
            if (string.IsNullOrEmpty(items.code_student))
                return Ok(new { message = "Không được bỏ trống mã sinh viên", success = false });
            var CheckLop = await db.Students.Where(x => x.code_student.ToLower().Trim() == items.code_student.ToLower().Trim()).FirstOrDefaultAsync();
            if (CheckLop != null)
                return Ok(new { message = "Sinh viên này đã tồn tại trong chương trình đào tạo, vui lòng kiểm tra lại", success = false });

            var new_record = new Student
            {
                name_student = items.name_student,
                code_student = items.code_student,
                id_class = items.id_class,
                time_up = unixTimestamp,
                tim_cre = unixTimestamp
            };
            db.Students.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost("info-sinh-vien")]
        public async Task<IActionResult> InfoClass([FromBody] StudentDTOs items)
        {
            var CheckClass = await db.Students
                .Where(x => x.id_student == items.id_student)
                .Select(x => new
                {
                    x.id_student,
                    x.id_class,
                    x.name_student,
                    x.code_student,
                })
                .FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin sinh viên, vui lòng kiểm tra lại", success = false });
            return Ok(new { data = CheckClass, success = true });
        }
        [HttpPost("update-sinh-vien")]
        public async Task<IActionResult> UpdateClass([FromBody] StudentDTOs items)
        {
            var CheckClass = await db.Students.Where(x => x.id_student == items.id_student).FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin sinh viên, vui lòng kiểm tra lại", success = false });
            CheckClass.name_student = items.name_student;
            CheckClass.code_student = items.code_student;
            CheckClass.id_class = items.id_class;
            CheckClass.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost("delete-sinh-vien")]
        public async Task<IActionResult> DeleteClass([FromBody] StudentDTOs items)
        {
            var CheckClass = await db.Students.Where(x => x.id_student == items.id_student).FirstOrDefaultAsync();
            if (CheckClass == null)
                return Ok(new { message = "Không tìm thấy thông tin sinh viên, vui lòng kiểm tra lại", success = false });
            db.Students.Remove(CheckClass);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        [HttpPost("upload-excel-danh-sach-sinh-vien")]
        public async Task<IActionResult> UploadExcelMonHoc(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Ok(new { message = "Vui lòng chọn file Excel.", success = false });

            if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                return Ok(new { message = "Chỉ hỗ trợ upload file Excel.", success = false });

            try
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return Ok(new { message = "Không tìm thấy worksheet trong file Excel", success = false });

                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var code_sv = worksheet.Cells[row, 2].Text?.Trim();
                    var ten_sv = worksheet.Cells[row, 3].Text?.Trim();
                    var ten_lop = worksheet.Cells[row, 4].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(code_sv))
                        continue;

                    Class CheckLop = null;
                    if (!string.IsNullOrWhiteSpace(ten_lop))
                    {
                        CheckLop = await db.Classes
                            .FirstOrDefaultAsync(x => x.name_class.ToLower().Trim() == ten_lop.ToLower().Trim());

                        if (CheckLop == null)
                        {
                            return Ok(new
                            {
                                message = $"Tên lớp '{ten_lop}' không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại",
                                success = false
                            });
                        }
                    }

                    var check_sv = await db.Students
                        .FirstOrDefaultAsync(x => x.code_student.ToLower().Trim() == code_sv.ToLower().Trim());

                    if (check_sv == null)
                    {
                        check_sv = new Student
                        {
                            code_student = code_sv.ToUpper(),
                            name_student = string.IsNullOrWhiteSpace(ten_sv) ? null : ten_sv.ToUpper(),
                            id_class = CheckLop?.id_class,
                            tim_cre = unixTimestamp,
                            time_up = unixTimestamp
                        };

                        db.Students.Add(check_sv);
                    }
                    else
                    {
                        check_sv.name_student = string.IsNullOrWhiteSpace(ten_sv) ? check_sv.name_student : ten_sv.ToUpper();
                        check_sv.id_class = CheckLop?.id_class ?? check_sv.id_class;
                        check_sv.time_up = unixTimestamp;
                    }

                    await db.SaveChangesAsync();
                }

                return Ok(new { message = "Import dữ liệu thành công", success = true });
            }
            catch (Exception ex)
            {
                return Ok(new { message = $"Lỗi khi đọc file Excel: {ex.Message}", success = false });
            }
        }


        [HttpPost]
        [Route("export-danh-sach-sinh-vien")]
        public async Task<IActionResult> ExportCourse([FromBody] StudentDTOs items)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var query = db.Students.Where(x => x.id_classNavigation.id_program == items.id_program).AsQueryable();
            if (items.id_class > 0)
            {
                query = query.Where(x => x.id_class == items.id_class);
            }
            var totalRecords = await db.Students
              .Where(x => x.id_classNavigation.id_program == items.id_program).CountAsync();
            var LoadClass = await query
                .OrderByDescending(x => x.id_class)
                .Select(x => new
                {
                    x.id_student,
                    x.code_student,
                    x.name_student,
                    x.id_classNavigation.name_class,
                    x.id_classNavigation.id_programNavigation.name_program,
                    x.time_up,
                    x.tim_cre
                })
                .ToListAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DanhSachSinhVien");

            string[] headers = {
                    "STT","Mã sinh viên","Tên sinh viên","Thuộc lớp","Thuộc CTĐT","Ngày tạo","Cập nhật"
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
                ws.Cells[row, 2].Value = item.code_student;
                ws.Cells[row, 3].Value = item.name_student;
                ws.Cells[row, 4].Value = item.name_class;
                ws.Cells[row, 5].Value = item.name_program;
                ws.Cells[row, 6].Value = ConvertUnix(item.tim_cre);
                ws.Cells[row, 7].Value = ConvertUnix(item.time_up);
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
