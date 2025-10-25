using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.IdentityModel.Logging;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Hubs;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.ComponentModel;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/faculty")]
    [ApiController]
    public class FacultyApiAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private readonly IHubContext<ImportHub> _hubContext;
        public FacultyApiAdmin(QLDCContext _db, IHubContext<ImportHub> hubContext)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            _hubContext = hubContext;
        }
        [HttpGet]
        [Route("loadsnambydonvi")]
        public async Task<IActionResult> LoadOptionFaculty()
        {
            var GetItems = await db.Years
                .Select(x => new
                {
                    value = x.id_year,
                    text = x.name_year
                })
                .ToListAsync();
            if (GetItems.Count > 0)
            {
                return Ok(new { data = GetItems, success = true });
            }
            else
            {
                return Ok(new { message = "Không có dữ liệu", success = false });
            }
        }
        [HttpPost]
        [Route("loadsdonvibynam/{id}")]
        public async Task<IActionResult> LoadData(int id, [FromBody] DataTableRequest request)
        {
            var query = db.Faculties
                .Where(x => x.id_year == id)
                .Select(x => new
                {
                    x.id_faculty,
                    x.code_faciulty,
                    x.name_faculty,
                    x.time_cre,
                    x.time_up,
                    x.id_yearNavigation.name_year
                });
            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var keyword = request.SearchText.ToLower().Trim();
                query = query.Where(x =>
                    x.name_faculty.ToLower().Contains(keyword) ||
                    (x.code_faciulty ?? "").ToLower().Contains(keyword));
            }
            var result = await DataTableHelper.GetDataTableAsync(query, request);
            return Ok(result);
        }

        [HttpPost]
        [Route("them-moi-don-vi")]
        public async Task<IActionResult> AddNew([FromBody] FacultyDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_faciulty))
                return Ok(new { message = "Không được bỏ trống Mã đơn vị", success = false });
            if (string.IsNullOrEmpty(items.name_faculty))
                return Ok(new { message = "Không được bỏ trống Tên đơn vị", success = false });

            var new_record = new Faculty
            {
                code_faciulty = items.code_faciulty,
                name_faculty = items.name_faculty,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                id_year = items.id_year
            };
            db.Faculties.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-don-vi")]
        public async Task<IActionResult> Info([FromBody] FacultyDTOs items)
        {
            var GetItems = await db.Faculties
                .Where(x => x.id_faculty == items.id_faculty)
                .Select(x => new
                {
                    x.id_faculty,
                    x.code_faciulty,
                    x.name_faculty,
                })
                .FirstOrDefaultAsync();
            return Ok(GetItems);
        }
        [HttpPost]
        [Route("update-don-vi")]
        public async Task<IActionResult> Updated([FromBody] FacultyDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_faciulty))
                return Ok(new { message = "Không được bỏ trống Mã đơn vị", success = false });
            if (string.IsNullOrEmpty(items.name_faculty))
                return Ok(new { message = "Không được bỏ trống Tên đơn vị", success = false });
            var checkItems = await db.Faculties
                .FirstOrDefaultAsync(x => x.id_faculty == items.id_faculty);
            checkItems.code_faciulty = items.code_faciulty;
            checkItems.name_faculty = items.name_faculty;
            checkItems.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpDelete]
        [Route("xoa-thong-tin-don-vi/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var CheckItems = await db.Faculties.FirstOrDefaultAsync(x => x.id_faculty == id);
            if (CheckItems == null)
            {
                return Ok(new { message = "Không tìm thấy thông tin đơn vị", success = false });
            }
            db.Faculties.Remove(CheckItems);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }


        [HttpPost("upload-excel-khoa-vien-truong")]
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
                            var ma_khoa = worksheet.Cells[row, 2].Text?.Trim();
                            var ten_khoa = worksheet.Cells[row, 3].Text?.Trim();
                            var ten_nam_hoc = worksheet.Cells[row, 4].Text?.Trim();

                            if (string.IsNullOrEmpty(ten_nam_hoc))
                                continue;

                            var check_nam_hoc = await db.Years
                                .FirstOrDefaultAsync(x => x.name_year.ToLower().Trim() == ten_nam_hoc.ToLower());

                            if (check_nam_hoc == null)
                            {
                                return Ok(new
                                {
                                    message = $"Năm học {ten_nam_hoc} không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại.",
                                    success = false
                                });
                            }

                            var check_khoa = await db.Faculties
                                .FirstOrDefaultAsync(x =>
                                    x.code_faciulty.ToLower().Trim() == ma_khoa.ToLower() &&
                                    x.name_faculty.ToLower().Trim() == ten_khoa.ToLower());

                            if (check_khoa == null)
                            {
                                check_khoa = new Faculty
                                {
                                    code_faciulty = string.IsNullOrWhiteSpace(ma_khoa) ? null : ma_khoa.ToUpper(),
                                    name_faculty = ten_khoa,
                                    id_year = check_nam_hoc.id_year,
                                    time_cre = unixTimestamp,
                                    time_up = unixTimestamp
                                };
                                db.Faculties.Add(check_khoa);
                            }
                            else
                            {
                                check_khoa.time_up = unixTimestamp;
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
