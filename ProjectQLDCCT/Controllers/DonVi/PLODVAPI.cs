using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Authorize(Policy = "DonVi")]
    [Route("api/donvi/program-learning-outcome")]
    [ApiController]
    public class PLODVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public PLODVAPI(QLDCContext _db)
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
        [Route("load-ctdt-thuoc-dv")]
        public async Task<IActionResult> LoadCTDTThuocDV()
        {
            GetFaculty = await GetUserPermissionFaculties();
            var LoadCTDTThuocDV = await db.TrainingPrograms
                .Where(x => x.id_faculty == GetFaculty.FirstOrDefault())
                .Select(x => new
                {
                    x.id_program,
                    x.name_program,
                })
                .ToListAsync();
            return Ok(LoadCTDTThuocDV);
        }
        [HttpPost]
        [Route("load-danh-sach-chuan-dau-ra-ctdt")]
        public async Task<IActionResult> LoadPLO([FromBody] PLODTOs items)
        {
            var totalRecords = await db.ProgramLearningOutcomes.Where(x => x.Id_Program == items.Id_Program).CountAsync();
            var GetItems = await db.ProgramLearningOutcomes
              .Where(x => x.Id_Program == items.Id_Program)
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
            GetFaculty = await GetUserPermissionFaculties();

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
