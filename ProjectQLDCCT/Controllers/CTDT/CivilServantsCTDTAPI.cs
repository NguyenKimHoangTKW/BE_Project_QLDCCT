using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Authorize(Policy = "CTDT")]
    [Route("api/ctdt/civil-servants")]
    [ApiController]
    public class CivilServantsCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CivilServantsCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("loads-danh-sach-can-bo-vien-chuc")]
        public async Task<IActionResult> LoadCBVC([FromBody] CivilServantsDTOs items)
        {
            var excludeIds = new int?[] { 2, 3 };

            var baseQuery = db.CivilServants
                .Where(cs => cs.id_program == items.id_program &&
                             !db.Users.Any(u => u.email == cs.email && excludeIds.Contains(u.id_type_users)))
                .Select(cs => new
                {
                    cs.id_civilSer,
                    cs.code_civilSer,
                    cs.fullname_civilSer,
                    cs.email,
                    cs.birthday,
                    ProgramName = cs.id_programNavigation.name_program,
                    cs.time_up,
                    cs.time_cre,
                    count_teacher_subjects = db.TeacherBySubjects.Count(tbs =>
                        tbs.id_user ==
                        db.Users
                            .Where(u => u.email == cs.email && !excludeIds.Contains(u.id_type_users))
                            .Select(u => u.id_users)
                            .FirstOrDefault())
                });

            var totalRecords = await baseQuery.CountAsync();

            var data = await baseQuery
                .OrderByDescending(x => x.id_civilSer)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });

        }

        [HttpPost]
        [Route("them-moi-can-bo-vien-chuc")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });
            var CheckNameCV = await db.CivilServants
                .Where(x => items.id_program == x.id_program && x.code_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim() && x.fullname_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckNameCV != null)
                return Ok(new { message = "Cán bộ viên chức này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var CheckEmailCV = await db.CivilServants
                .Where(x => items.id_program == x.id_program && x.email.ToLower().Trim() == items.email.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckEmailCV != null)
                return Ok(new { message = "Email này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var new_record = new CivilServant
            {
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                code_civilSer = items.code_civilSer,
                birthday = items.birthday,
                id_program = items.id_program,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CivilServants.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-can-bo-vien-chuc")]
        public async Task<IActionResult> InfoCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
                .Where(x => x.id_civilSer == items.id_civilSer)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.email,
                    x.code_civilSer,
                    x.birthday,
                    x.fullname_civilSer,
                    x.id_program
                })
                .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            return Ok(new { data = checkInfo, success = true });
        }
        [HttpPost]
        [Route("update-can-bo-vien-chuc")]
        public async Task<IActionResult> UpdateCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });

            var checkInfo = await db.CivilServants
              .Where(x => x.id_civilSer == items.id_civilSer)
              .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            checkInfo.fullname_civilSer = items.fullname_civilSer;
            checkInfo.code_civilSer = items.code_civilSer;
            checkInfo.email = items.email;
            checkInfo.birthday = items.birthday;
            checkInfo.id_program = items.id_program;
            checkInfo.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-can-bo-vien-chuc")]
        public async Task<IActionResult> DeleteCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
            .Where(x => x.id_civilSer == items.id_civilSer)
            .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            db.CivilServants.Remove(checkInfo);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        [HttpPost]
        [Route("loads-list-de-cuong-by-gv")]
        public async Task<IActionResult> LoadList([FromBody] CivilServantsDTOs items)
        {
            var checkCivilSer = await db.CivilServants
                .FirstOrDefaultAsync(x => x.id_civilSer == items.id_civilSer);

            if (checkCivilSer == null)
                return Ok(new { message = "Không tìm thấy thông tin Cán bộ viên chức", success = false });

            var listCourse = await (
                from tbs in db.TeacherBySubjects
                join u in db.Users on tbs.id_user equals u.id_users
                join cs in db.CivilServants on u.email equals cs.email
                join c in db.Courses on tbs.id_course equals c.id_course
                where u.email == checkCivilSer.email
                select new
                {
                    tbs.id_teacherbysubject,
                    c.id_course,
                    c.code_course,
                    c.name_course,
                    GroupCourse = c.id_gr_courseNavigation.name_gr_course,
                    Semester = c.id_semesterNavigation.code_semester + " - " + c.id_semesterNavigation.name_semester,
                    KeyYearSemester = c.id_key_year_semesterNavigation.code_key_year_semester + " - " + c.id_key_year_semesterNavigation.name_key_year_semester,
                    c.credits,
                    c.totalTheory,
                    c.totalPractice,
                    isCourse = c.id_isCourseNavigation.name,
                }
            )
            .OrderByDescending(x => x.id_teacherbysubject)
            .ToListAsync();

            return Ok(new
            {
                success = true,
                data = listCourse
            });
        }
    }
}
