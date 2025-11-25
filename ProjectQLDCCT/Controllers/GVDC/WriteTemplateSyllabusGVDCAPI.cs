using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Google.Cloud.AIPlatform.V1;
using Google.Protobuf.WellKnownTypes;
using HtmlToOpenXml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers.Services;
using ProjectQLDCCT.Helpers.SignalR;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

namespace ProjectQLDCCT.Controllers.GVDC
{
    [Authorize(Policy = "GVDC")]
    [Route("api/gvdc/write-template-syllabus")]
    [ApiController]
    public class WriteTemplateSyllabusGVDCAPI : ControllerBase
    {
        private readonly ILmStudioService _lmStudio;
        private readonly IHubContext<SyllabusHub> _hub;
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public class PromptRequest
        {
            public string sectionTitle { get; set; }
            public string courseName { get; set; }
            public string customPrompt { get; set; }
        }


      
        public WriteTemplateSyllabusGVDCAPI(QLDCContext _db, ILmStudioService lmStudio, IHubContext<SyllabusHub> hub)
        {
            db = _db;
            _hub = hub;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            _lmStudio = lmStudio;
        }
        private int GetUserIdFromJWT()
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
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Token không chứa id_users hợp lệ.");

            return userId;
        }

        private async Task<string> GetUserPermissionNameCodeGV()
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
            if (!int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedAccessException("Token không chứa id_users hợp lệ.");

            var email = await db.Users
                .Where(x => x.id_users == userId)
                .Select(x => x.email)
                .FirstOrDefaultAsync();

            if (email == null)
                return "";

            var loadPermission = await db.CivilServants
                .Where(g => g.email == email)
                .Select(g => g.code_civilSer + " - " + g.fullname_civilSer)
                .FirstOrDefaultAsync();

            return loadPermission ?? "";
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

        [HttpPost("suggest-stream")]
        public async Task SuggestStream([FromBody] PromptRequest req)
        {
            // Không dùng IActionResult nữa, mà stream trực tiếp vào Response
            HttpContext.Response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            HttpContext.Response.Headers.Add("Cache-Control", "no-cache");
            HttpContext.Response.Headers.Add("X-Accel-Buffering", "no"); // tránh buffer ở reverse proxy (nếu có)

            var sectionTitle = req.sectionTitle;
            var courseName = req.courseName;

            // Prompt mặc định (bạn chỉnh lại theo ý)
            string defaultPrompt =
        $@"Bạn là chuyên gia giáo dục đại học, có kinh nghiệm biên soạn đề cương chi tiết học phần.

Hãy viết MỘT đoạn văn dài, gồm nhiều đoạn, văn phong học thuật – chuẩn mực – logic, nội dung rõ ràng, có mở đầu – triển khai – kết luận.

YÊU CẦU:
Không bullet
Không đánh số
Không gạch đầu dòng
Không liệt kê
Không sử dụng văn phong trò chuyện
Không dùng từ ngữ đời thường
Không lặp lại câu
Không viết ngắn
Tối thiểu khoảng 800–1000 chữ

Viết nội dung cho mục ""{sectionTitle}"" của học phần ""{courseName}"", theo ngữ cảnh giáo dục đại học.";

            var finalPrompt = string.IsNullOrWhiteSpace(req.customPrompt)
                ? defaultPrompt
                : req.customPrompt;

            // Tạo payload cho LM Studio (OpenAI-compatible)
            var payload = new
            {
                model = "fusechat-gemma-2-9b-instruct",
                stream = true,   // BẬT STREAM
                messages = new[]
                {
            new {
                role = "user",
                content = finalPrompt
            }
        }
            };

            var httpFactory = HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
            var client = httpFactory.CreateClient("LmStudio");

            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            using var httpReq = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            // Quan trọng: ResponseHeadersRead để bắt đầu đọc stream sớm
            using var httpRes = await client.SendAsync(
                httpReq,
                HttpCompletionOption.ResponseHeadersRead,
                HttpContext.RequestAborted
            );

            httpRes.EnsureSuccessStatusCode();

            await using var responseStream = await httpRes.Content.ReadAsStreamAsync(HttpContext.RequestAborted);
            using var reader = new StreamReader(responseStream);

            // LM Studio stream giống OpenAI: từng dòng "data: {json}" và kết thúc "[DONE]"
            while (!reader.EndOfStream && !HttpContext.RequestAborted.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data:"))
                    continue;

                var data = line.Substring("data:".Length).Trim();

                if (data == "[DONE]")
                    break;

                try
                {
                    using var doc = JsonDocument.Parse(data);
                    var root = doc.RootElement;
                    var choices = root.GetProperty("choices");
                    if (choices.GetArrayLength() == 0) continue;

                    // Với streaming kiểu OpenAI: choices[0].delta.content
                    if (choices[0].TryGetProperty("delta", out var delta)
                        && delta.TryGetProperty("content", out var contentElement))
                    {
                        var content = contentElement.GetString();
                        if (!string.IsNullOrEmpty(content))
                        {
                            await HttpContext.Response.WriteAsync(content);
                            await HttpContext.Response.Body.FlushAsync();
                        }
                    }
                }
                catch
                {
                    // Nếu parse fail thì bỏ qua chunk đó
                }
            }
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
            var listInt = new int?[] { 1, 7 };
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
            var GetCourse = await db.Syllabi.Where(x => x.id_syllabus == dto.id_syllabus)
                .Select(x => x.id_teacherbysubjectNavigation.id_course)
                .FirstOrDefaultAsync();

            var CheckTypeSyllabus = await db.Syllabi
                .Where(x => x.id_teacherbysubjectNavigation.id_user == userId && x.id_teacherbysubjectNavigation.id_course == GetCourse && !listInt.Contains(x.id_status))
                .ToListAsync();

            if (CheckTypeSyllabus.Any())
            {
                return Ok(new { message = "Bạn đang có đề cương đang được xử lý, không thể nộp thêm đề cương", success = false });
            }
            var CheckMappingCLO = await db.MappingCLOBySyllabi
                .Where(x => x.id_syllabus == dto.id_syllabus)
                .Select(x => x.id)
                .ToListAsync();
            if (!CheckMappingCLO.Any())
                return Ok(new { message = "Bạn chưa có dữ liệu ma trận CLO, không thể lưu", success = false });

            var CheckMappingPI = await db.MappingCLObyPIs
                .Where(x => CheckMappingCLO.Contains(x.id_CLoMapping ?? 0))
                .ToListAsync();
            if (!CheckMappingPI.Any())
                return Ok(new { message = "Bạn chưa có dữ liệu ma trận PI thuộc PLO, không thể lưu", success = false });
            var syllabus = await db.Syllabi
                .FirstOrDefaultAsync(x => x.id_syllabus == dto.id_syllabus);

            if (syllabus == null)
                return Ok(new { success = false, message = "Không tìm thấy syllabus!" });

            string json = JsonConvert.SerializeObject(dto.data);

            syllabus.syllabus_json = json;
            syllabus.time_up = unixTimestamp;
            syllabus.id_status = 2;
            syllabus.returned_content = null;
            syllabus.edit_content = null;
            syllabus.is_open_edit_final = 0;
            var GetNameGV = await GetUserPermissionNameCodeGV();
            var new_record_log = new Log_Syllabus
            {
                id_syllabus = syllabus.id_syllabus,
                content_value = $"Giảng viên {GetNameGV} vừa nộp đề cương chờ duyệt với phiên bản {syllabus.version}",
                log_time = unixTimestamp
            };
            db.Log_Syllabi.Add(new_record_log);
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





        // 1) Lưu draft 1 section
        [HttpPost("save-draft-section")]
        public async Task<IActionResult> SaveDraftSection([FromBody] SaveDraftSectionDTO dto)
        {
            var userId = GetUserIdFromJWT();

            var draft = await db.SyllabusDrafts
                .FirstOrDefaultAsync(x => x.id_syllabus == dto.id_syllabus
                                       && x.section_code == dto.section_code);

            if (draft == null)
            {
                draft = new SyllabusDraft
                {
                    id_syllabus = dto.id_syllabus,
                    section_code = dto.section_code,
                    content_code = dto.content,
                    id_user = userId,
                    update_time = unixTimestamp
                };
                db.SyllabusDrafts.Add(draft);
            }
            else
            {
                draft.content_code = dto.content;
                draft.id_user = userId;
                draft.update_time = unixTimestamp;
            }

            await db.SaveChangesAsync();

            var userName = await db.Users
                .Where(u => u.id_users == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            // broadcast cho tất cả người đang mở syllabus này
            // ⭐ realtime notify
            await _hub.Clients.Group(dto.id_syllabus.ToString())
                .SendAsync("SectionDraftUpdated", new
                {
                    id_syllabus = dto.id_syllabus,
                    section_code = dto.section_code,
                    content_code = dto.content
                });

            return Ok(new { success = true });
        }

        // 2) Load draft hiện tại của syllabus (mỗi section 1 content)
        [HttpPost("load-drafts")]
        public async Task<IActionResult> LoadDrafts([FromBody] LoadDraftDTO dto)
        {
            var drafts = await db.SyllabusDrafts
                .Where(x => x.id_syllabus == dto.id_syllabus)
                .Select(x => new
                {
                    x.section_code,
                    x.content_code,
                    x.id_user,
                    x.update_time
                })
                .ToListAsync();

            return Ok(new { success = true, data = drafts });
        }
        public class SyllabusSectionDTO
        {
            public string section_code { get; set; }
            public string section_name { get; set; }
            public string value { get; set; }
            public string allow_input { get; set; }
            public string contentType { get; set; }
            public string dataBinding { get; set; }
        }
        [HttpPost("save-final-from-draft")]
        public async Task<IActionResult> SaveFinalFromDraft([FromBody] SaveFinalFromDraftDTO dto)
        {
            var userId = GetUserIdFromJWT();

            // check quyền: chỉ GV chính / người được quyền mới cho save
            var isOwner = await db.Syllabi
                .AnyAsync(x => x.id_syllabus == dto.id_syllabus
                            && x.id_teacherbysubjectNavigation.id_user == userId);

            if (!isOwner)
                return Ok(new { success = false, message = "Bạn không có quyền lưu bản final của đề cương này." });

            var syllabus = await db.Syllabi
                .FirstOrDefaultAsync(x => x.id_syllabus == dto.id_syllabus);

            if (syllabus == null)
                return NotFound(new { success = false, message = "Không tìm thấy đề cương." });

            // Deserialize về DTO chuẩn
            List<SyllabusSectionDTO> sections = new();
            if (!string.IsNullOrEmpty(syllabus.syllabus_json))
            {
                sections = System.Text.Json.JsonSerializer.Deserialize<List<SyllabusSectionDTO>>(syllabus.syllabus_json);
            }

            var drafts = await db.SyllabusDrafts
                .Where(x => x.id_syllabus == dto.id_syllabus)
                .ToListAsync();

            // MERGE content từ draft
            foreach (var sec in sections)
            {
                var draft = drafts.FirstOrDefault(d => d.section_code == sec.section_code);
                if (draft != null)
                    sec.value = draft.content_code ?? "";
            }

            // Save Final
            syllabus.syllabus_json = System.Text.Json.JsonSerializer.Serialize(sections);
            syllabus.id_status = 4;
            syllabus.time_up = unixTimestamp;

            var userName = await db.Users
                .Where(u => u.id_users == userId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync();

            // Log
            db.Log_Syllabi.Add(new Log_Syllabus
            {
                id_syllabus = syllabus.id_syllabus,
                content_value = $"Giảng viên {userName} vừa lưu bản final đề cương.",
                log_time = unixTimestamp
            });

            // clear draft
            db.SyllabusDrafts.RemoveRange(drafts);

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã lưu bản final đề cương từ tất cả nội dung draft.",
            });
        }

        //ssssssssssssss
        [HttpPost("save-content-draft")]
        public async Task<IActionResult> SaveDraftContent([FromBody] SaveSyllabusDraftContentDTO req)
        {
            var exist = await db.Syllabus_Drafts
                .FirstOrDefaultAsync(x => x.id_syllabus == req.id_syllabus);

            if (exist == null)
            {
                exist = new Syllabus_Draft
                {
                    id_syllabus = req.id_syllabus,
                    draft_json = req.draft_json,
                    update_time = unixTimestamp
                };
                db.Syllabus_Drafts.Add(exist);
            }
            else
            {
                exist.draft_json = req.draft_json;
                exist.update_time = unixTimestamp;
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost("save-sections")]
        public async Task<IActionResult> SaveDraftSections([FromBody] SaveSyllabusDraftSectionDTO req)
        {
            var exist = await db.Syllabus_Draft_Sections
                .FirstOrDefaultAsync(x => x.id_syllabus == req.id_syllabus);
            if (exist == null)
            {
                exist = new Syllabus_Draft_Section
                {
                    id_syllabus = req.id_syllabus,
                    section_json = req.section_json,
                    update_time = unixTimestamp
                };
                db.Syllabus_Draft_Sections.Add(exist);
            }
            else
            {
                exist.section_json =req.section_json;
                exist.update_time = unixTimestamp;
            }

            await db.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpPost("load-draft-section")]
        public async Task<IActionResult> LoadDraft([FromBody] SyllabusDTOs items) 
        {
            var sec = await db.Syllabus_Draft_Sections
                .FirstOrDefaultAsync(x => x.id_syllabus == items.id_syllabus);

            var cont = await db.Syllabus_Drafts
                .FirstOrDefaultAsync(x => x.id_syllabus ==items.id_syllabus);

            return Ok(new
            {
                success = true,
                sections = sec?.section_json,
                content = cont?.draft_json
            });
        }

    }
}
