using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/donvi/key-semester")]
    [ApiController]
    public class KeySemesteDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public KeySemesteDVAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("loads-danh-sach-khoa-hoc")]
        public async Task<IActionResult> LoadListKeySemester([FromBody] KeySemesterDTOs items)
        {
            var totalRecords = await db.KeyYearSemesters
                .Where(x => x.id_faculty == items.id_faculty).CountAsync();

            var KeySemesterList = await db.KeyYearSemesters
                .Where(x => x.id_faculty == items.id_faculty)
                .OrderByDescending(x => x.id_key_year_semester)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_key_year_semester,
                    x.code_key_year_semester,
                    x.name_key_year_semester,
                    x.time_cre,
                    x.time_up,
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = KeySemesterList,
                currentPage = items.Page,
                pageSize = items.PageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }

        [HttpPost]
        [Route("them-moi-khoa-hoc")]
        public async Task<IActionResult> ThemMoiKeySemester([FromBody] KeySemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Tên khóa học", success = false });
            if (string.IsNullOrEmpty(items.code_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã khóa học", success = false });
            var CheckKeySemester = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.name_key_year_semester.ToLower().Trim() == items.name_key_year_semester.ToLower().Trim());
            if (CheckKeySemester != null)
            {
                return Ok(new { message = "Khóa học này đã tồn tại, vui lòng kiểm tra lại", success = false });
            }

            var new_record = new KeyYearSemester
            {
                id_faculty = items.id_faculty,
                code_key_year_semester = items.code_key_year_semester,
                name_key_year_semester = items.name_key_year_semester,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.KeyYearSemesters.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-khoa-hoc")]
        public async Task<IActionResult> InfoKeySemester([FromBody] KeySemesterDTOs items)
        {
            var checkKeySemester = await db.KeyYearSemesters
                .Where(x => x.id_key_year_semester == items.id_key_year_semester)
                .Select(x => new
                {
                    x.code_key_year_semester,
                    x.name_key_year_semester,
                })
                .FirstOrDefaultAsync();
            if (checkKeySemester == null)
                return Ok(new { message = "Không tìm thấy thông tin khóa học", success = false });

            return Ok(new { data = checkKeySemester, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-khoa-hoc")]
        public async Task<IActionResult> UpdateKeySemester([FromBody] KeySemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Tên khóa học", success = false });
            if (string.IsNullOrEmpty(items.code_key_year_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã khóa học", success = false });
            var CheckKeySemester = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);
            if (CheckKeySemester == null)
                return Ok(new { message = "Không tìm thấy thông tin Khóa học", success = false });
            CheckKeySemester.code_key_year_semester = items.code_key_year_semester;
            CheckKeySemester.name_key_year_semester = items.name_key_year_semester;
            CheckKeySemester.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-khoa-hoc")]
        public async Task<IActionResult> DeleteKeySemester([FromBody] KeySemesterDTOs items)
        {
            var CheckCourse = await db.CourseByKeys.FirstOrDefaultAsync(x => x.id_key_semester == items.id_key_year_semester);
            if (CheckCourse != null)
                return Ok(new { message = "Khóa học này đang tồn tại dữ liệu trong Môn học, không thể xóa", success = false });

            var CheckKey = await db.KeyYearSemesters.FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);
            if (CheckKey == null)
                return Ok(new { message = "Không tìm thấy thông tin Khóa học", success = false });

            db.KeyYearSemesters.Remove(CheckKey);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
