using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Shared
{
    [Route("api/preview")]
    [ApiController]
    public class PreviewTemplateSyllabusAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public PreviewTemplateSyllabusAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("preview-template")]
        public async Task<IActionResult> PreviewTemplate([FromBody] SyllabusDTOs items)
        {
            var idCourse = await (
                from s in db.Syllabi
                join t in db.TeacherBySubjects on s.id_teacherbysubject equals t.id_teacherbysubject
                where s.id_syllabus == items.id_syllabus
                select t.id_course
            ).FirstOrDefaultAsync();
            var checkTemplate = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => new
                {
                    x.syllabus_json,
                    course = x.id_teacherbysubjectNavigation.id_courseNavigation.name_course,
                    status = x.id_status == 4,
                    is_open = db.OpenSyllabusWindowsCourses.Any(g => g.id_course == idCourse && g.is_open == 1)
                })
                .FirstOrDefaultAsync();

            if (checkTemplate == null)
                return Ok(new
                {
                    message = "Không tìm thấy thông tin biểu mẫu",
                    success = false
                });

            return Ok(new
            {
                data = checkTemplate,
                success = true,
                message = "Tải mẫu đề cương thành công"
            });
        }
        [HttpPost]
        [Route("load-mapping-clo-by-de-cuong")]
        public async Task<IActionResult> LoadMappingCLOBySyllabus([FromBody] MappingCLOBySyllabusDTOs items)
        {
            var CheckClo = await db.MappingCLOBySyllabi
                .Where(x => items.id_syllabus == x.id_syllabus)
                .Select(x => new
                {
                    x.id,
                    x.map_clo,
                    x.description
                })
                .ToListAsync();
            return Ok(CheckClo);
        }

        [HttpPost]
        [Route("get-mapping-clo-pi")]
        public async Task<IActionResult> GetMappingCLOPI([FromBody] MappingCLOBySyllabusDTOs items)
        {
            var result = await (
                from clo in db.MappingCLOBySyllabi
                join map in db.MappingCLObyPIs
                    on clo.id equals map.id_CLoMapping
                where clo.id_syllabus == items.id_syllabus
                select new
                {
                    id_CLoMapping = map.id_CLoMapping,
                    Id_PI = map.Id_PI,
                    Id_Level = map.Id_Level
                }
            ).ToListAsync();

            return Ok(result);
        }
        [HttpPost]
        [Route("loads-plo-hoc-phan")]
        public async Task<IActionResult> LoadPloHP([FromBody] SyllabusDTOs items)
        {
            var checkCou = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => x.id_teacherbysubjectNavigation.id_course)
                .FirstOrDefaultAsync();
            var checkCourse = await db.Courses
                .Where(x => x.id_course == checkCou)
                .FirstOrDefaultAsync();

            if (checkCourse == null)
                return NotFound(new { success = false, message = "Không tìm thấy học phần" });

            var listPlo = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Program == checkCourse.id_program)
                .Select(x => new { x.Id_Plo, x.code })
                .ToListAsync();

            var mappedPloIds = await db.ContributionMatrices
                .Where(cm => cm.Id_PINavigation.Id_PLONavigation.Id_Program == checkCourse.id_program && cm.id_course == checkCou)
                .Select(cm => cm.Id_PINavigation.Id_PLO)
                .Distinct()
                .ToListAsync();

            var totalPloMapped = mappedPloIds.Count;

            var listData = new List<object>();

            foreach (var plo in listPlo)
            {
                if (!mappedPloIds.Contains(plo.Id_Plo)) continue;

                var piList = await db.ContributionMatrices
                    .Where(cm => cm.Id_PINavigation.Id_PLO == plo.Id_Plo && cm.id_course == checkCou)
                    .Select(cm => new
                    {
                        id_PI = cm.Id_PI,
                        pi_code = cm.Id_PINavigation.code,
                        level_code = cm.id_levelcontributonNavigation.Code,
                        des_level = cm.id_levelcontributonNavigation.Description
                    })
                    .ToListAsync();

                var piDistinct = piList
                    .GroupBy(x => x.pi_code)
                    .Select(g => g.First())
                    .ToList();

                if (piDistinct.Count > 0)
                {
                    listData.Add(new
                    {
                        plo_code = plo.code,
                        count_pi = piDistinct.Count,
                        pi_list = piDistinct
                    });
                }
            }

            return Ok(new { success = true, count_plo = totalPloMapped, data = listData });
        }

        [HttpPost("export-word-html")]
        public IActionResult ExportWordFromHtml([FromBody] HtmlToDocxRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Html))
                return BadRequest("HTML không hợp lệ");

            using MemoryStream mem = new MemoryStream();
            using (WordprocessingDocument wordDoc =
                WordprocessingDocument.Create(mem, WordprocessingDocumentType.Document, true))
            {
                MainDocumentPart mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new Document(new Body());

                var converter = new HtmlConverter(mainPart);
                converter.ParseHtml(req.Html);

                mainPart.Document.Save();
            }

            mem.Position = 0;

            return File(
                mem.ToArray(),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "Syllabus.docx"
            );
        }

        public class HtmlToDocxRequest
        {
            public string Html { get; set; }
        }
    }
}
