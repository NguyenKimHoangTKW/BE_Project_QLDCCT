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

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Route("api/gvdc/write-template-syllabus")]
    [ApiController]
    public class WriteTemplateSyllabusGVDCAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public WriteTemplateSyllabusGVDCAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("preview-template")]
        public async Task<IActionResult> PreviewTemplate([FromBody] SyllabusDTOs items)
        {
            var checkTemplate = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => new
                {
                    x.syllabus_json,
                    status = x.id_status == 4 
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
        [Route("preview-course-objectives")]
        public async Task<IActionResult> LoadsPreviewCO([FromBody] SyllabusDTOs items)
        {
            var checkFac = await db.Syllabi
              .Where(x => x.id_syllabus == items.id_syllabus)
              .Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_programNavigation.id_faculty)
              .FirstOrDefaultAsync();
            var loadsdata = await db.CourseObjectives
                .Where(x => x.id_faculty == checkFac)
                .Select(x => new
                {
                    x.name_CO,
                    x.describe_CO,
                    x.typeOfCapacity
                })
                .ToListAsync();

            return Ok(new { success = true, data = loadsdata });
        }

        [HttpPost]
        [Route("preview-course-learning-outcomes")]
        public async Task<IActionResult> LoadsPreviewCLO([FromBody] SyllabusDTOs items)
        {
            var checkFac = await db.Syllabi
              .Where(x => x.id_syllabus == items.id_syllabus)
              .Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_programNavigation.id_faculty)
              .FirstOrDefaultAsync();

            var loadsdata = await db.CourseLearningOutcomes
                .Where(x => x.id_faculty == checkFac)
                .Select(x => new
                {
                    x.name_CLO,
                    x.describe_CLO,
                    x.bloom_level
                })
                .ToListAsync();
            return Ok(new { success = true, data = loadsdata });
        }
        [HttpPost]
        [Route("preview-program-learning-outcome")]
        public async Task<IActionResult> LoadsPreviewPLO([FromBody] PLODTOs items)
        {
            var checkPro = await db.Syllabi
                .Where(x => x.id_syllabus == items.id_syllabus)
                .Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_program)
                .FirstOrDefaultAsync();
            var listData = new List<object>();
            var LoadPLO = await db.ProgramLearningOutcomes
                .OrderBy(x => x.order_index)
                .Where(x => x.Id_Program == checkPro)
                .ToListAsync();
            foreach (var item in LoadPLO)
            {
                var GetListPI = await db.PerformanceIndicators
                    .Where(x => x.Id_PLO == item.Id_Plo)
                    .OrderBy(x => x.order_index)
                    .Select(x => new
                    {
                        x.code,
                        x.Description,
                    }).ToListAsync();
                listData.Add(new
                {
                    code_plo = item.code,
                    description_plo = item.Description,
                    count_pi = GetListPI.Count,
                    pi = GetListPI
                });
            }
            return Ok(listData);
        }
        [HttpPost]
        [Route("preview-level-contribution")]
        public async Task<IActionResult> loadLevelContribution([FromBody] LevelContributionDTOs items)
        {
            var checkFac = await db.Syllabi
             .Where(x => x.id_syllabus == items.id_syllabus)
             .Select(x => x.id_teacherbysubjectNavigation.id_courseNavigation.id_programNavigation.id_faculty)
             .FirstOrDefaultAsync();
            var LoadData = await db.LevelContributions
                .Where(x => x.id_faculty == checkFac)
                .Select(x => new
                {
                    x.id,
                    x.Code,
                    x.Description
                })
                .ToListAsync();
            return Ok(LoadData);
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
        [Route("save-mapping-clo")]
        public async Task<IActionResult> SaveMappingCLO([FromBody] MappingCLOBySyllabusDTOs items)
        {

            var exist = await db.MappingCLOBySyllabi
                   .FirstOrDefaultAsync(x => x.id == items.id);
            if (exist == null)
            {
                if (string.IsNullOrEmpty(items.description))
                    return Ok(new { message = "Nội dung của CLO không được bỏ trống", success = false });
                var checkMapping = await db.MappingCLOBySyllabi
                .Where(x => x.id_syllabus == items.id_syllabus && x.map_clo == items.map_clo)
                .FirstOrDefaultAsync();
                if (checkMapping != null)
                    return Ok(new { message = "Bị trùng CLO, vui lòng kiểm tra lại", success = false });
                var newData = new MappingCLOBySyllabus
                {
                    id_syllabus = items.id_syllabus,
                    map_clo = items.map_clo,
                    description = items.description
                };

                db.MappingCLOBySyllabi.Add(newData);
            }
            else
            {
                exist.map_clo = items.map_clo;
                exist.description = items.description;
            }
            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost]
        [Route("delete-mapping-clo")]
        public async Task<IActionResult> DeleteMappingCLO([FromBody] MappingCLOBySyllabusDTOs items)
        {
            if (items.id == null)
                return Ok(new { message = "Thiếu id_mapping", success = false });

            var exist = await db.MappingCLOBySyllabi
                .FirstOrDefaultAsync(x => x.id == items.id);

            db.MappingCLOBySyllabi.Remove(exist);
            await db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost]
        [Route("save-mapping-clo-pi")]
        public async Task<IActionResult> SaveMappingCLOPI([FromBody] List<MappingCLObyPIDTOs> items)
        {
            int idSyllabus = (int)items.First().id_syllabus;

            var idCourse = await (
                from s in db.Syllabi
                join t in db.TeacherBySubjects on s.id_teacherbysubject equals t.id_teacherbysubject
                where s.id_syllabus == idSyllabus
                select t.id_course
            ).FirstOrDefaultAsync();
            if (idCourse == 0)
            {
                return Ok(new
                {
                    success = false,
                    message = "Không xác định được id_course từ syllabus. Vui lòng kiểm tra dữ liệu."
                });
            }

            foreach (var item in items)
            {
                var exist = await db.MappingCLObyPIs
                    .FirstOrDefaultAsync(x =>
                        x.id_CLoMapping == item.id_CLoMapping &&
                        x.Id_PI == item.Id_PI);

                if (item.Id_Level > 0)
                {
                    var validCodes = await db.ContributionMatrices
                        .Where(x => x.Id_PI == item.Id_PI && x.id_course == idCourse)
                        .Select(x => x.id_levelcontributonNavigation.Code)
                        .ToListAsync();

                    string selected = item.code_Level.Trim();
                    bool isMultiSelect = selected.Contains(",");

                    bool isValid = validCodes.Any(code =>
                    {
                        var normalized = code.Replace(" ", "");
                        var normalizedSelected = selected.Replace(" ", "");

                        if (isMultiSelect)
                        {
                            return string.Equals(normalized, normalizedSelected, StringComparison.OrdinalIgnoreCase);
                        }
                        else
                        {
                            return normalized
                                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Contains(selected, StringComparer.OrdinalIgnoreCase);
                        }
                    });

                    if (!isValid)
                    {
                        return Ok(new
                        {
                            success = false,
                            message = $"Mức độ đóng góp '{item.code_Level}' của PI {item.Id_PI} không khớp với bảng tham chiếu."
                        });
                    }
                }

                if (item.Id_Level == 0)
                {
                    if (exist != null)
                        db.MappingCLObyPIs.Remove(exist);

                    continue;
                }

                if (exist != null)
                {
                    exist.Id_Level = item.Id_Level;
                }
                else
                {
                    db.MappingCLObyPIs.Add(new MappingCLObyPI
                    {
                        id_CLoMapping = item.id_CLoMapping,
                        Id_PI = item.Id_PI,
                        Id_Level = item.Id_Level
                    });
                }
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true, message = "Saved!" });
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
        [HttpPost]
        [Route("save-final")]
        public async Task<IActionResult> SaveFinalSyllabus([FromBody] SaveFinalSyllabusDTO dto)
        {
            var syllabus = await db.Syllabi
                .FirstOrDefaultAsync(x => x.id_syllabus == dto.id_syllabus);

            if (syllabus == null)
                return Ok(new { success = false, message = "Không tìm thấy syllabus!" });

            string json = JsonConvert.SerializeObject(dto.data);

            syllabus.syllabus_json = json;
            syllabus.time_up = unixTimestamp;
            syllabus.id_status = 2;
            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã lưu thành công toàn bộ nội dung đề cương!"
            });
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
