using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Authorize(Policy = "Admin")]
    [Route("api/admin/group_course")]
    [ApiController]
    public class GroupCourseApiAdmin : ControllerBase
    {

        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public GroupCourseApiAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("danh-sach-nhom-hoc-phan")]
        public async Task<IActionResult> DanhSachNhomHocPhan([FromBody] DataTableRequest request)
        {
            var GetData = db.Group_Courses
                .Select(x => new
                {
                    x.id_gr_course,
                    x.name_gr_course,
                    x.time_cre,
                    x.time_up
                });
            var results = await DataTableHelper.GetDataTableAsync(GetData, request);
            return Ok(results);
        }
        [HttpPost]
        [Route("them-moi-nhom-hoc-phan")]
        public async Task<IActionResult> ThemMoiNhomHocPhan([FromBody] GroupCourseDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_gr_course))
                return Ok(new { message = "Không được bỏ trống Tên nhóm học phần", success = true });
            var CheckData = await db.Group_Courses.FirstOrDefaultAsync(x => x.name_gr_course.ToLower().Trim() == items.name_gr_course.ToLower().Trim());
            if (CheckData != null)
                return Ok(new { message = "Nhóm học phần này đã tồn tại, vui lòng kiểm tra lại", success = false });
            var new_record = new Group_Course
            {
                name_gr_course = items.name_gr_course,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.Group_Courses.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-nhom-hoc-phan")]
        public async Task<IActionResult> InfoNhomHocPhan([FromBody] GroupCourseDTOs items)
        {
            var checkItems = await db.Group_Courses
                .Where(x => x.id_gr_course == items.id_gr_course)
                .Select(x => new
                {
                    x.id_gr_course,
                    x.name_gr_course
                })
                .FirstOrDefaultAsync();
            return Ok(checkItems);
        }
        [HttpPost]
        [Route("update-nhom-hoc-phan")]
        public async Task<IActionResult> UpdateNhomHocPhan([FromBody] GroupCourseDTOs items)
        {
            if (string.IsNullOrEmpty(items.name_gr_course))
                return Ok(new { message = "Không được bỏ trống Tên nhóm học phần", success = true });
            var CheckItems = await db.Group_Courses.FirstOrDefaultAsync(x => x.id_gr_course == items.id_gr_course);
            if (CheckItems == null)
            {
                return Ok(new { message = "Không tìm thấy dữ liệu", success = false });
            }

            CheckItems.name_gr_course = items.name_gr_course;
            CheckItems.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("delete-nhom-hoc-phan")]
        public async Task<IActionResult> XoaNhomHocPhan([FromBody] GroupCourseDTOs items)
        {
            var CheckCourse = await db.Courses.FirstOrDefaultAsync(x => x.id_gr_course == items.id_gr_course);
            if (CheckCourse != null)
                return Ok(new { message = "Nhóm học phần này đang tồn tại môn học, không thể xóa", success = false });
            var CheckItems = await db.Group_Courses.FirstOrDefaultAsync(x => x.id_gr_course == items.id_gr_course);
            if (CheckItems == null)
                return Ok(new { message = "Không tìm thấy dữ liệu", success = false });
            db.Group_Courses.Remove(CheckItems);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
