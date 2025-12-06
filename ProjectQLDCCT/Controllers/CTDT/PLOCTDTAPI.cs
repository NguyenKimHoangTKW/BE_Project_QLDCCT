using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Authorize(Policy = "CTDT")]
    [Route("api/ctdt/program-learning-outcome")]
    [ApiController]
    public class PLOCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public PLOCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("load-option-plo")]
        public async Task<IActionResult> LoadCTDTThuocDV([FromBody] PLODTOs items)
        {
            if (items == null || items.Id_Program == 0)
                return BadRequest(new { message = "Thiếu tham số Id_Program" });

            var checkCTDT = await db.TrainingPrograms
                .FirstOrDefaultAsync(x => x.id_program == items.Id_Program);

            if (checkCTDT == null)
                return BadRequest(new { message = "Không tìm thấy chương trình đào tạo (CTDT)" });

            if (checkCTDT.id_faculty == null)
                return BadRequest(new { message = "CTDT không thuộc khoa nào" });

            var GetList = await db.KeyYearSemesters
                .Where(x => x.id_faculty == checkCTDT.id_faculty)
                .OrderByDescending(x => x.id_key_year_semester)
                .Select(x => new
                {
                    x.id_key_year_semester,
                    x.name_key_year_semester
                })
                .ToListAsync();

            return Ok(new { keySemester = GetList });
        }

        [HttpPost]
        [Route("load-danh-sach-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> LoadPLO([FromBody] PLODTOs items)
        {
            var query =  db.ProgramLearningOutcomes.Where(x => x.Id_Program == items.Id_Program).AsQueryable();

            if (!string.IsNullOrEmpty(items.searchTerm))
            {
                string keyword = items.searchTerm.ToLower();
                query = query.Where(x =>
                x.code.ToLower().Contains(keyword) ||
                x.Description.ToLower().Contains(keyword));
            }
            var totalRecords = await query.Where(x => x.Id_Program == items.Id_Program).CountAsync();
            var GetItems = await query
              .Where(x => x.Id_Program == items.Id_Program && items.id_key_semester == x.id_key_semester)
              .OrderBy(x => x.order_index)
              .Skip((items.Page - 1) * items.PageSize)
              .Take(items.PageSize)
              .Select(x => new
              {
                  x.Id_Plo,
                  x.Id_Program,
                  x.code,
                  x.Description,
                  x.order_index,
                  x.time_cre,
                  x.time_up,
                  total_pi = db.PerformanceIndicators.Count(pi => pi.Id_PLO == x.Id_Plo)
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
        [Route("them-moi-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> AddNew([FromBody] PLODTOs items)
        {
            if (string.IsNullOrEmpty(items.code))
                return Ok(new { message = "Không được bỏ trống Mã chuẩn đầu ra chương trình đào tạo", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống Nội dung chuẩn đầu ra chương trình đào tạo", success = false });

            var list = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Program == items.Id_Program)
                .OrderBy(x => x.order_index)
                .ToListAsync();

            var CheckRecord = list.FirstOrDefault(x => x.code.ToLower().Trim() == items.code.ToLower().Trim());
            if (CheckRecord != null)
                return Ok(new { message = "Chuẩn đầu ra chương trình đào tạo này đã tồn tại, vui lòng kiểm tra lại", success = false });

            int insertIndex = Math.Max(1, Math.Min((int)items.order_index, list.Count + 1));

            var newRecord = new ProgramLearningOutcome
            {
                Id_Program = items.Id_Program,
                code = items.code.Trim(),
                Description = items.Description.Trim(),
                id_key_semester = items.id_key_semester,
                time_cre = unixTimestamp,
                time_up = unixTimestamp
            };

            list.Insert(insertIndex - 1, newRecord);

            for (int i = 0; i < list.Count; i++)
            {
                list[i].order_index = i + 1;
            }
            db.ProgramLearningOutcomes.Add(newRecord);
            await db.SaveChangesAsync();

            return Ok(new { message = "Thêm mới dữ liệu thành công và đã sắp xếp lại thứ tự", success = true });
        }

        [HttpPost]
        [Route("info-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> InfoPLO([FromBody] PLODTOs items)
        {
            var CheckPLO = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Plo == items.Id_Plo)
                .Select(x => new
                {
                    x.Id_Plo,
                    x.Id_Program,
                    x.code,
                    x.id_key_semester,
                    x.order_index,
                    x.Description
                })
                .FirstOrDefaultAsync();
            if (CheckPLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra học chương trình đào tạo", success = false });
            return Ok(new { data = CheckPLO, success = true });
        }
        [HttpPost]
        [Route("update-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> UpdatePLO([FromBody] PLODTOs items)
        {
            var checkPLO = await db.ProgramLearningOutcomes
                .FirstOrDefaultAsync(x => x.Id_Plo == items.Id_Plo);

            if (checkPLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra chương trình đào tạo", success = false });

            if (string.IsNullOrEmpty(items.code))
                return Ok(new { message = "Không được bỏ trống mã chuẩn đầu ra", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống nội dung chuẩn đầu ra", success = false });

            var list = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Program == checkPLO.Id_Program)
                .OrderBy(x => x.order_index)
                .ToListAsync();

            checkPLO.code = items.code.Trim();
            checkPLO.Description = items.Description.Trim();
            checkPLO.time_up = unixTimestamp;

            list.Remove(checkPLO);

            int newIndex = Math.Max(1, Math.Min((int)items.order_index, list.Count + 1));
            list.Insert(newIndex - 1, checkPLO);

            for (int i = 0; i < list.Count; i++)
            {
                list[i].order_index = i + 1;
            }

            await db.SaveChangesAsync();

            return Ok(new { message = "Cập nhật dữ liệu thành công và đã sắp xếp lại thứ tự", success = true });
        }

        [HttpPost]
        [Route("xoa-du-lieu-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> DeletePLO([FromBody] PLODTOs items)
        {
            var checkPLO = await db.ProgramLearningOutcomes
                .FirstOrDefaultAsync(x => x.Id_Plo == items.Id_Plo);

            if (checkPLO == null)
                return Ok(new { message = "Không tìm thấy thông tin chuẩn đầu ra chương trình đào tạo", success = false });

            var idProgram = checkPLO.Id_Program;

            db.ProgramLearningOutcomes.Remove(checkPLO);
            await db.SaveChangesAsync();

            var remaining = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Program == idProgram)
                .OrderBy(x => x.order_index)
                .ToListAsync();

            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].order_index = i + 1;
            }

            await db.SaveChangesAsync();

            return Ok(new { message = "Xóa dữ liệu thành công và đã sắp xếp lại thứ tự", success = true });
        }

        [HttpPost]
        [Route("load-pi-thuoc-plo")]
        public async Task<IActionResult> LoadPITrongPLO([FromBody] PIDTOs items)
        {
            var totalRecords = await db.PerformanceIndicators
               .Where(x => x.Id_PLO == items.Id_PLO)
               .CountAsync();
            var GetItems = await db.PerformanceIndicators
               .Where(x => x.Id_PLO == items.Id_PLO)
               .OrderBy(x => x.order_index)
               .Skip((items.Page - 1) * items.PageSize)
               .Take(items.PageSize)
               .Select(x => new
               {
                   x.Id_PI,
                   x.code,
                   x.Description,
                   x.order_index,
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
        [Route("them-moi-pi-thuoc-plo")]
        public async Task<IActionResult> ThemMoiPIThuocPLO([FromBody] PIDTOs items)
        {
            if (string.IsNullOrEmpty(items.code))
                return Ok(new { message = "Không được bỏ trống trường Tên PI", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống trường Nội dung PI", success = false });
            var checkstt = await db.PerformanceIndicators.Where(x => x.Id_PLO == items.Id_PLO && x.order_index == items.order_index).FirstOrDefaultAsync();
            if (checkstt != null)
                return Ok(new { message = "Bị trùng số thứ tự, vui lòng kiểm tra lại", success = false });
            var checkrecord = await db.PerformanceIndicators.Where(x => x.code.ToLower().Trim() == items.code.ToLower().Trim()).FirstOrDefaultAsync();
            if (checkrecord != null)
                return Ok(new { message = "PI này đã tồn tại, vui lòng kiểm tra lại", success = false });
            var new_record = new PerformanceIndicator
            {
                Id_PLO = items.Id_PLO,
                code = items.code,
                Description = items.Description,
                order_index = items.order_index,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.PerformanceIndicators.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }

        [HttpPost]
        [Route("thong-tin-pi-thuoc-plo")]
        public async Task<IActionResult> InfoPIThuocPLO([FromBody] PIDTOs items)
        {
            var checkRecord = await db.PerformanceIndicators
                .Where(x => x.Id_PI == items.Id_PI)
                .Select(x => new
                {
                    x.code,
                    x.Description,
                    x.order_index,
                })
                .FirstOrDefaultAsync();
            if (checkRecord == null)
                return Ok(new { message = "Không tìm thấy thông tin PI", success = false });
            return Ok(new { data = checkRecord, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-pi-thuoc-plo")]
        public async Task<IActionResult> UpdatePIThuocPLO([FromBody] PIDTOs items)
        {
            if (string.IsNullOrEmpty(items.code))
                return Ok(new { message = "Không được bỏ trống trường Tên PI", success = false });
            if (string.IsNullOrEmpty(items.Description))
                return Ok(new { message = "Không được bỏ trống trường Nội dung PI", success = false });

            var checkRecord = await db.PerformanceIndicators
                .FirstOrDefaultAsync(x => x.Id_PI == items.Id_PI);

            if (checkRecord == null)
                return Ok(new { message = "Không tìm thấy thông tin PI", success = false });
            checkRecord.code = items.code;
            checkRecord.Description = items.Description;
            checkRecord.time_up = unixTimestamp;

            var list = await db.PerformanceIndicators
                .OrderBy(x => x.order_index)
                .ToListAsync();

            list.Remove(checkRecord);

            int newIndex = Math.Max(1, Math.Min((int)items.order_index, list.Count + 1));
            list.Insert(newIndex - 1, checkRecord);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].order_index = i + 1;
            }
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-pi-thuoc-plo")]
        public async Task<IActionResult> DeletePIThuocPLO([FromBody] PIDTOs items)
        {
            var checkRecord = await db.PerformanceIndicators
                .FirstOrDefaultAsync(x => x.Id_PI == items.Id_PI);

            if (checkRecord == null)
                return Ok(new { message = "Không tìm thấy thông tin PI", success = false });

            var idPLO = checkRecord.Id_PLO;
            db.PerformanceIndicators.Remove(checkRecord);
            await db.SaveChangesAsync();
            var remaining = await db.PerformanceIndicators
                .Where(x => x.Id_PLO == idPLO)
                .OrderBy(x => x.order_index)
                .ToListAsync();
            for (int i = 0; i < remaining.Count; i++)
            {
                remaining[i].order_index = i + 1;
            }
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
