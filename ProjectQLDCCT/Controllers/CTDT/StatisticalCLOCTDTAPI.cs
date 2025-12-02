using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/ctdt/statistical-plo")]
    [ApiController]
    public class StatisticalCLOCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public StatisticalCLOCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public class SyllabusSection
        {
            public string section_code { get; set; }
            public string section_name { get; set; }
            public string value { get; set; }
            public string allow_input { get; set; }
            public string contentType { get; set; }
            public string dataBinding { get; set; }
        }

        [HttpPost]
        [Route("load-option-thong-ke-nhap-lieu")]
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
        [Route("thong-ke-nhap-lieu-plo")]
        public async Task<IActionResult> LoadStaCLO([FromBody] StatisticalCLODTOs items)
        {
            var CheckSyllabus = await db.Syllabi
                .Include(x => x.id_teacherbysubjectNavigation)
                    .ThenInclude(t => t.id_courseNavigation)
                .Where(x =>
                    x.id_teacherbysubjectNavigation.id_courseNavigation.id_key_year_semester == items.id_key_semester &&
                    x.id_status == 4 &&
                    x.id_teacherbysubjectNavigation.id_courseNavigation.id_program == items.id_program
                )
                .ToListAsync();

            var ListData = new List<object>();

            foreach (var it in CheckSyllabus)
            {
                var GetCLO = await db.MappingCLOBySyllabi
                    .Where(x => x.id_syllabus == it.id_syllabus)
                    .ToListAsync();

                var sections = new List<SyllabusSection>();

                if (!string.IsNullOrWhiteSpace(it.syllabus_json))
                {
                    try
                    {
                        sections = Newtonsoft.Json.JsonConvert
                            .DeserializeObject<List<SyllabusSection>>(it.syllabus_json);
                    }
                    catch
                    {
                    }
                }

                var describeCourse = sections
                    .FirstOrDefault(s => s.section_name == "Mô tả học phần")
                    ?.value ?? "";
                var MoTaHocPhan = sections
                    .FirstOrDefault(s =>
                        s.section_code == "3" ||
                       (!string.IsNullOrWhiteSpace(s.dataBinding) &&
                        s.dataBinding.Replace("\u00A0", " ").Trim()
                            .Contains("CO - Biểu mẫu Mục tiêu học phần"))
                    )?.value ?? "";
                var formattedCLO = string.Join("<br/>",
                    GetCLO.Select(c => $"{c.map_clo}: {c.description}")
                );
                ListData.Add(new
                {
                    name_course = it.id_teacherbysubjectNavigation.id_courseNavigation.code_course
                        + " - "
                        + it.id_teacherbysubjectNavigation.id_courseNavigation.name_course,

                    describe_course = describeCourse,
                    mo_ta = MoTaHocPhan,             
                    clo = formattedCLO               
                });
            }

            return Ok(new { data = ListData, success = true });
        }

    }
}
