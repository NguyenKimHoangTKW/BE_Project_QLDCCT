using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Drawing.Printing;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/donvi/semester")]
    [ApiController]
    public class HocKyDVApi : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public HocKyDVApi(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("loads-danh-sach-hoc-ky")]
        public async Task<IActionResult> LoadsHocKy([FromBody] SemesterDTOs items)
        {
            var totalRecords = await db.Semesters
                .Where(x => x.id_faculty == items.id_faculty)
                .CountAsync();

            var hocKyList = await db.Semesters
                .Where(x => x.id_faculty == items.id_faculty)
                .OrderByDescending(x => x.id_semester)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_semester,
                    x.code_semester,
                    x.name_semester,
                    x.tim_cre,
                    x.time_up
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = hocKyList,
                currentPage = items.Page,
                pageSize = items.PageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-hoc-kyf")]
        public async Task<IActionResult> ThemsMoiHocKy([FromBody] SemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_semester))
                return Ok(new { message = "Không được bỏ trống trường tên Học kỳ", success = false });
            if (string.IsNullOrEmpty(items.code_semester))
                return Ok(new { message = "Không được bỏ trống trường Mã Học kỳ", success = false });
            var checkHocKyRecord = await db.Semesters.Where(x => x.name_semester.ToLower().Trim() == items.name_semester.ToLower().Trim()).FirstOrDefaultAsync();
            if (checkHocKyRecord != null)
                return Ok(new { message = "Học kỳ này đã tồn tại trong CSDL, vui lòng kiểm tra lại", success = false });
            var new_record = new Semester
            {
                name_semester = items.name_semester,
                id_faculty = items.id_faculty,
                code_semester = items.code_semester,
                tim_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.Semesters.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới học kỳ thành công", success = true });
        }
        [HttpPost]
        [Route("info-hoc-ky")]
        public async Task<IActionResult> InfoHocKys([FromBody] SemesterDTOs items)
        {
            if (items.id_semester == null | items.id_semester == 0)
                return Ok(new { message = "Không tìm thấy thông tin học kỳ", success = false });
            var CheckHocKys = await db.Semesters
                .Where(x => x.id_semester == items.id_semester)
                .Select(x => new
                {
                    x.id_faculty,
                    x.name_semester,
                    x.code_semester,
                    x.id_semester
                })
                .FirstOrDefaultAsync();
            return Ok(new { data = CheckHocKys, success = true });
        }
        [HttpPost]
        [Route("update-hoc-kys")]
        public async Task<IActionResult> UpdateHocKys([FromBody] SemesterDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_semester))
                return Ok(new { message = "Không được bỏ trống trường tên Học kỳ", success = false });
            var checkHocKyRecord = await db.Semesters.Where(x => x.id_semester == items.id_semester).FirstOrDefaultAsync();

            checkHocKyRecord.name_semester = items.name_semester;
            checkHocKyRecord.code_semester = items.code_semester;
            checkHocKyRecord.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("delete-hoc-kys")]
        public async Task<IActionResult> DeleteHocKys([FromBody] SemesterDTOs items)
        {
            var CheckMonHoc = await db.CourseByKeys.FirstOrDefaultAsync(x => x.id_semester == items.id_semester);
            if (CheckMonHoc != null)
                return Ok(new { message = "Học kỳ này đang tồn tại môn học của chương trình, không thể xóa", success = false });
            var CheckHockys = await db.Semesters.FirstOrDefaultAsync(x => x.id_semester == items.id_semester);
            if (CheckHockys == null)
                return Ok(new { message = "Không tìm thấy thông tin Học kỳ", success = false });

            db.Semesters.Remove(CheckHockys);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
