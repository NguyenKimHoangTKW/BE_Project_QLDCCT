using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers.Services;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Client
{
    [Route("api")]
    [ApiController]
    public class SearchSyllabusByCodeStudent : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public SearchSyllabusByCodeStudent(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpPost]
        [Route("de-cuong-chi-tiet")]
        public async Task<IActionResult> FilterCourse([FromBody] FilterClientSyllabus items)
        {
            if (string.IsNullOrEmpty(items.mssv))
                return Ok(new { message = "Không được bỏ trống Mã số sinh viên", success = false });

            var CheckMssv = await db.Students
                .Include(x => x.id_classNavigation)
                .ThenInclude(c => c.id_programNavigation)
                .FirstOrDefaultAsync(x => x.code_student == items.mssv);

            if (CheckMssv == null)
                return Ok(new { message = "Mã số sinh viên không tồn tại hoặc sai định dạng", success = false });

            int idProgram = CheckMssv.id_classNavigation.id_program ?? 0;
            int idFaculty = CheckMssv.id_classNavigation.id_programNavigation.id_faculty ?? 0;

            string mssv = items.mssv.Trim();
            if (mssv.Length < 2)
                return Ok(new { success = false, message = "MSSV không hợp lệ" });

            string result = "D" + mssv.Substring(0, 2);

            var checkKeyYear = await db.KeyYearSemesters
                .Where(x => x.id_faculty == idFaculty && x.code_key_year_semester == result)
                .Select(x => x.id_key_year_semester)
                .FirstOrDefaultAsync();

            if (checkKeyYear == 0)
                return Ok(new { success = false, message = "Không tìm thấy khóa học phù hợp" });

            var semesters = await db.Semesters
                .Where(x => x.id_faculty == idFaculty)
                .OrderBy(x => x.code_semester)
                .ToListAsync();

            var ListData = new List<object>();

            foreach (var semester in semesters)
            {
                var loadCourse = await db.Courses
                    .Where(x =>
                        x.id_semester == semester.id_semester &&
                        x.id_key_year_semester == checkKeyYear &&
                        x.id_program == idProgram)
                    .Select(x => new
                    {
                        x.id_course,
                        x.code_course,
                        x.name_course,
                        name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                        name_isCourse = x.id_isCourseNavigation.name,
                        x.totalPractice,
                        x.totalTheory,
                        x.credits,
                        count_syllabus = x.TeacherBySubjects.Count(),

                        time_open = db.OpenSyllabusWindowsCourses
                                    .Where(g => g.id_course == x.id_course)
                                    .Select(g => g.open_time)
                                    .FirstOrDefault(),

                        time_close = db.OpenSyllabusWindowsCourses
                                    .Where(g => g.id_course == x.id_course)
                                    .Select(g => g.close_time)
                                    .FirstOrDefault(),

                        is_syllabus = db.Syllabi.Any(g =>
                            g.id_teacherbysubjectNavigation.id_course == x.id_course &&
                            g.id_status == 4)
                    })
                    .ToListAsync();

                if (loadCourse.Count > 0)
                {
                    ListData.Add(new
                    {
                        id_se = semester.id_semester,
                        name_se = semester.name_semester,
                        code_se = semester.code_semester,
                        course = loadCourse
                    });
                }
            }

            return Ok(new { data = ListData, message = "Lọc dữ liệu thành công", success = true });
        }
        [HttpPost("preview-de-cuong")]
        public IActionResult PreviewSyllabus(
     [FromBody] SyllabusDTOs items,
     [FromServices] IPdfService pdfService)
        {
            if (items == null || items.id_course == 0)
                return BadRequest(new { success = false, message = "Thiếu dữ liệu đầu vào." });

            var html = db.Syllabi
                .Where(x => x.id_teacherbysubjectNavigation.id_course == items.id_course &&
                            x.id_status == 4)
                .Select(x => x.html_export_word)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(html))
                return Ok(new { success = false, message = "Không có dữ liệu" });

            try
            {
                var pdfBytes = pdfService.ConvertHtmlToPdf(html);

                return File(pdfBytes, "application/pdf", "DeCuongChiTiet.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi tạo PDF",
                    detail = ex.Message
                });
            }
        }

    }
}
