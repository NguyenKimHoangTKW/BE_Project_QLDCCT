using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Authorize(Policy = "Admin")]
    [Route("api/admin/program")]
    [ApiController]
    public class TrainingProgramAPIAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public TrainingProgramAPIAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpGet]
        [Route("loads-select-don-vi-by-year")]
        public async Task<IActionResult> LoadDonViByCTDT()
        {
            var GetDV = await db.Faculties
                .Select(x => new
                {
                    value = x.id_faculty,
                    name = x.name_faculty
                })
                .ToListAsync();
            return Ok(GetDV);
        }
        [HttpPost]
        [Route("loads-ctdt-thuoc-don-vi/{idDonvi}")]
        public async Task<IActionResult> LoadData(int idDonvi, [FromBody] DataTableRequest items)
        {
            var query = db.TrainingPrograms.AsQueryable();

            if (idDonvi == 0)
            {
                query = query;
            }
            else
            {
                query = query.Where(x => x.id_faculty == idDonvi);
            }
            var _query = query
                .Select(x => new
                {
                    x.id_program,
                    x.code_program,
                    x.name_program,
                    x.time_up,
                    x.time_cre,
                    x.id_facultyNavigation.name_faculty
                });

            if (!string.IsNullOrEmpty(items.SearchText))
            {
                var keyword = items.SearchText.ToLower().Trim();
                _query = _query.Where(x => x.name_program.ToLower().Contains(keyword) ||
                (x.code_program ?? "").ToLower().Contains(keyword) ||
                x.name_faculty.ToLower().Contains(keyword));
            }
            var result = await DataTableHelper.GetDataTableAsync(_query, items);
            return Ok(result);
        }
        [HttpPost]
        [Route("them-moi-ctdt-thuoc-nam")]
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
        [HttpDelete]
        [Route("xoa-du-lieu-ctdt/{id}")]
        public async Task<IActionResult> DeleteCTDT(int id)
        {
            var CheckItems = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.id_program == id);
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
                            var ten_don_vi = worksheet.Cells[row, 2].Text?.Trim();
                            var ma_ctdt = worksheet.Cells[row, 3].Text?.Trim();
                            var ten_ctdt = worksheet.Cells[row, 4].Text?.Trim();
                            var checkDonVi = await db.Faculties
                                    .FirstOrDefaultAsync(x =>
                                        (x.name_faculty ?? "").ToLower().Trim() == ten_don_vi.ToLower().Trim());
                            if (string.IsNullOrEmpty(ten_don_vi))
                                continue;

                            if (checkDonVi == null)
                            {
                                return Ok(new
                                {
                                    message = $"Đơn vị '{ten_don_vi}' không tồn tại hoặc sai định dạng, vui lòng kiểm tra lại ở dòng {row}.",
                                    success = false
                                });
                            }
                            var check_ctdt = await db.TrainingPrograms
                                .FirstOrDefaultAsync(x =>
                                   (x.code_program ?? "").ToLower().Trim() == (ma_ctdt ?? "").ToLower() &&
                                    ((x.name_program ?? "") ?? "").ToLower().Trim() == (ten_ctdt ?? "").ToLower());

                            if (check_ctdt == null)
                            {
                                check_ctdt = new TrainingProgram
                                {
                                    id_faculty = string.IsNullOrEmpty(ten_don_vi) ? null : checkDonVi.id_faculty,
                                    code_program = string.IsNullOrWhiteSpace(ma_ctdt) ? null : ma_ctdt.ToUpper(),
                                    name_program = ten_ctdt,
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
    }
}
