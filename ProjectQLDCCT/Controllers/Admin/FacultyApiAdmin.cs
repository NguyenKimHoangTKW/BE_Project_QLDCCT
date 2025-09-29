using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/faculty")]
    [ApiController]
    public class FacultyApiAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public FacultyApiAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
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
            var result = await DataTableHelper.GetDataTableAsync(query, request,
                x => x.code_faciulty,
                x => x.name_faculty
                );
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
    }
}
