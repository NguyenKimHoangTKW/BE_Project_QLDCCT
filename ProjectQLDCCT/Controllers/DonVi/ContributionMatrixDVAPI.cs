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
    [Route("api/donvi/contribution-matrix")]
    [ApiController]
    public class ContributionMatrixDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public ContributionMatrixDVAPI(QLDCContext _db)
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
        [Route("loads-option-cm")]
        public async Task<IActionResult> LoadCTDTByDV()
        {
            GetFaculty = await GetUserPermissionFaculties();
            var ListCTDT = await db.TrainingPrograms
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id_program,
                    x.name_program
                })
                .ToListAsync();

            var ListKey = await db.KeyYearSemesters
                .OrderByDescending(x => x.code_key_year_semester)
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id_key_year_semester,
                    name_key = x.code_key_year_semester + " - " + x.name_key_year_semester
                })
                .ToListAsync();
            return Ok(new { ctdt = ListCTDT, key_year = ListKey });
        }
        [HttpPost]
        [Route("loads-chuan-dau-ra-hoc-phan")]
        public async Task<IActionResult> loadsPLoPi([FromBody] PLODTOs items)
        {

            var LoadPLO = await db.ProgramLearningOutcomes
                .OrderBy(x => x.order_index)
                .Where(x => x.Id_Program == items.Id_Program && x.id_key_semester == items.id_key_semester)
                .ToListAsync();
            var ListData = new List<object>();
            foreach (var plo in LoadPLO)
            {
                var LoadPI = await db.PerformanceIndicators
                    .OrderBy(x => x.order_index)
                    .Where(x => x.Id_PLO == plo.Id_Plo)
                    .Select(x => new
                    {
                        x.Id_PI,
                        x.code,
                        x.Description
                    })
                    .ToListAsync();
                ListData.Add(new
                {
                    code_plo = plo.code,
                    des_plo = plo.Description,
                    count_pi = LoadPI.Count,
                    pi = LoadPI
                });
            }
            return Ok(ListData);
        }
        [HttpPost]
        [Route("loads-ma-tran-dong-gop")]
        public async Task<IActionResult> LoadHocPhan([FromBody] CourseDTOs items)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var checkKeySemester = await db.KeyYearSemesters
                .FirstOrDefaultAsync(x => x.id_key_year_semester == items.id_key_year_semester);

            int? filterYear = null;
            if (checkKeySemester != null && !string.IsNullOrEmpty(checkKeySemester.code_key_year_semester))
            {
                var suffix = new string(checkKeySemester.code_key_year_semester
                    .Where(char.IsDigit)
                    .ToArray());
                if (int.TryParse(suffix, out int yearSuffix))
                {
                    filterYear = 2000 + yearSuffix;
                }
            }
            var LoadSemesterQuery = db.Semesters
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0));

            if (filterYear.HasValue)
            {
                LoadSemesterQuery = LoadSemesterQuery
                    .Where(x => Convert.ToInt32(x.code_semester.Substring(0, 4)) >= filterYear.Value);
            }

            var LoadSemester = await LoadSemesterQuery
                .OrderBy(x => x.code_semester)
                .ToListAsync();

            var ListData = new List<object>();

            foreach (var semester in LoadSemester)
            {
                var loadCourse = await db.Courses
                    .Where(x =>
                        x.id_semester == semester.id_semester &&
                        x.id_key_year_semester == items.id_key_year_semester &&
                        x.id_program == items.id_program)
                    .Select(x => new
                    {
                        x.id_course,
                        x.code_course,
                        x.name_course,
                        name_gr_course = x.id_gr_courseNavigation.name_gr_course,
                        name_isCourse = x.id_isCourseNavigation.name,
                        x.totalPractice,
                        x.totalTheory
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
            return Ok(new { data = ListData, success = true });
        }

        [HttpPost("get-matrix")]
        public async Task<IActionResult> GetMatrix([FromBody] ContributionMatrixDTOs request)
        {
            GetFaculty = await GetUserPermissionFaculties();
            var levels = await db.LevelContributions
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id,
                    x.Code,
                    x.Description
                })
                .ToListAsync();
            var courses = await db.Courses
                .OrderBy(x => x.id_semesterNavigation.code_semester)
                .Where(x => x.id_program == request.id_program
                    && x.id_key_year_semester == request.id_key_year_semester)
                .Select(x => new
                {
                    x.id_course,
                    x.code_course,
                    x.name_course,
                    x.totalPractice,
                    x.totalTheory,
                    x.credits,
                    x.id_semesterNavigation.name_semester
                })
                .ToListAsync();
            var pis = await db.PerformanceIndicators
                .Where(x => x.Id_PLONavigation.Id_Program == request.id_program)
                .Select(x => new
                {
                    x.Id_PI,
                    x.code,
                    x.Description
                })
                .ToListAsync();
            var matrix = await db.ContributionMatrices
                .ToListAsync();
            var result = courses.Select(course => new
            {
                course.id_course,
                course.code_course,
                course.name_course,
                course.totalTheory,
                course.totalPractice,
                course.credits,
                name_se = course.name_semester,
                pi = pis.Select(pi =>
                {
                    var existing = matrix.FirstOrDefault(m =>
                        m.id_course == course.id_course && m.Id_PI == pi.Id_PI);

                    var matchedLevel = existing != null
                        ? levels.FirstOrDefault(l => l.id == existing.id_levelcontributon)
                        : null;

                    return new
                    {
                        id_PI = pi.Id_PI,
                        code_PI = pi.code,
                        id_level = matchedLevel?.id ?? 0,
                        level_code = matchedLevel?.Code ?? "",
                        level_description = matchedLevel?.Description ?? ""
                    };
                }).ToList()
            });
            return Ok(new
            {
                success = true,
                levels,
                data = result
            });
        }
        [HttpPost("save-matrix")]
        public async Task<IActionResult> SaveMatrix([FromBody] List<ContributionMatrixDTOs> items)
        {
            foreach (var item in items)
            {
                if (item.id_levelcontributon == 0)
                {
                    var toDelete = await db.ContributionMatrices
                        .Where(x => x.id_course == item.id_course && x.Id_PI == item.Id_PI)
                        .ToListAsync();

                    if (toDelete.Any())
                    {
                        db.ContributionMatrices.RemoveRange(toDelete);
                    }

                    continue;
                }
                var existing = await db.ContributionMatrices
                    .FirstOrDefaultAsync(x => x.id_course == item.id_course && x.Id_PI == item.Id_PI);

                if (existing != null)
                {
                    existing.id_levelcontributon = item.id_levelcontributon;
                    db.ContributionMatrices.Update(existing);
                }
                else
                {
                    db.ContributionMatrices.Add(new ContributionMatrix
                    {
                        id_course = item.id_course,
                        Id_PI = item.Id_PI,
                        id_levelcontributon = item.id_levelcontributon
                    });
                }
            }

            var result = await db.SaveChangesAsync();
            return Ok(new { success = true, affected = result, message = "Lưu thành công" });
        }

    }
}
