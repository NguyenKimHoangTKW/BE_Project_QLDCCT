using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.ConditionalFormatting.Contracts;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Authorize(Policy = "DonVi")]
    [Route("api/donvi/syllabustemplate")]
    [ApiController]
    public class TemplateSyllabusDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public TemplateSyllabusDVAPI(QLDCContext _db)
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
        [HttpPost]
        [Route("load-mau-de-cuong")]
        public async Task<IActionResult> TemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var loadPermision = await GetUserPermissionFaculties();
            var totalRecords = await db.SyllabusTemplates.Where(x => loadPermision.Contains(x.id_faculty ?? 0)).CountAsync();
            var GetItems = await db.SyllabusTemplates
                .Where(x => loadPermision.Contains(x.id_faculty ?? 0))
                .OrderByDescending(x => x.id_template)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_template,
                    x.template_name,
                    allow_input = x.is_default == 1 ? "Cho phép nhập liệu" : "Không cho phép nhập liệu",
                    x.time_up,
                    x.time_cre
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                data = GetItems,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-mau-de-cuong")]
        public async Task<IActionResult> ThemMoiTempalteSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var loadPermision = await GetUserPermissionFaculties();
            if (string.IsNullOrEmpty(items.template_name))
                return Ok(new { message = "Không được bỏ trống trường Tên mẫu đề cương", success = false });
            var CheckSyllabusTemplate = await db.SyllabusTemplates.FirstOrDefaultAsync(x => x.template_name.ToLower().Trim() == items.template_name.ToLower().Trim() && loadPermision.Contains(x.id_faculty ?? 0));
            if (CheckSyllabusTemplate != null)
                return Ok(new { message = "Tên mẫu đề cương này đã tồn tại, vui lòng kiểm tra lại.", success = false });

            var new_record = new SyllabusTemplate
            {
                template_name = items.template_name,
                is_default = items.is_default,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
                id_faculty = loadPermision.FirstOrDefault()
            };
            db.SyllabusTemplates.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-mau-de-cuong")]
        public async Task<IActionResult> InfoTemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var CheckInfo = await db.SyllabusTemplates
                .Where(x => x.id_template == items.id_template)
                .Select(x => new
                {
                    x.id_template,
                    x.template_name,
                    x.is_default,
                })
                .FirstOrDefaultAsync();
            if (CheckInfo == null)
                return Ok(new { message = "Không tìm thấy thông tin mẫu đề cương", success = false });
            return Ok(new { data = CheckInfo, success = true });
        }
        [HttpPost]
        [Route("cap-nhat-mau-de-cuong")]
        public async Task<IActionResult> UpdateTempalteSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            if (string.IsNullOrEmpty(items.template_name))
                return Ok(new { message = "Không được bỏ trống trường Tên mẫu đề cương", success = false });

            var checkTemplate = await db.SyllabusTemplates.FirstOrDefaultAsync(x => x.id_template == items.id_template);
            if (checkTemplate == null)
                return Ok(new { message = "Không tìm thấy thông tin Mẫu đề cương", success = false });

            checkTemplate.is_default = items.is_default;
            checkTemplate.template_name = items.template_name;
            checkTemplate.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-mau-de-cuong")]
        public async Task<IActionResult> DeleteTemplateSyllabus([FromBody] TemplateSyllabusDTOs items)
        {
            var CheckSection = await db.SyllabusTemplateSections.Where(x => x.id_template == items.id_template).Select(x => x.id_template).ToListAsync();
            var checkSyllabusSection = await db.SyllabusTemplateSections.Where(x => CheckSection.Contains(x.id_template)).ToListAsync();
            return Ok();
        }

        // Create Template Section

        [HttpGet]
        [Route("load-selected-template")]
        public async Task<IActionResult> LoadSelectedTemplate()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var loadListTemplateByFaculty = await db.SyllabusTemplates
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id_template,
                    x.template_name
                })
                .ToListAsync();
            return Ok(loadListTemplateByFaculty);
        }
        [HttpGet]
        [Route("load-option-template-section")]
        public async Task<IActionResult> LoadOption()
        {
            var ListContentType = await db.ContentTypes
                .Select(x => new
                {
                    x.id,
                    name = x.code + " - " + x.name
                })
                .ToListAsync();
            var ListDataBinding = await db.DataBindings
                .Select(x => new
                {
                    x.id,
                    name = x.code + " - " + x.name
                })
                .ToListAsync();
            return Ok(new
            {
                contentType = ListContentType,
                dataBinding = ListDataBinding
            });
        }
        [HttpPost]
        [Route("create-template-section")]
        public async Task<IActionResult> CreateSyllabusTemplateSection([FromBody] SyllabusTemplateSectionDTOs items)
        {
            if (string.IsNullOrWhiteSpace(items.section_code))
                return Ok(new { message = "Không được bỏ trống trường Số thứ tự mục chính", success = false });

            if (string.IsNullOrWhiteSpace(items.section_name))
                return Ok(new { message = "Không được bỏ trống trường Tên tiêu đề", success = false });

            bool existOrder = await db.SyllabusTemplateSections
                .AnyAsync(x => x.id_template == items.id_template && x.order_index == items.order_index);
            if (existOrder)
                return Ok(new { message = "Bị trùng thứ tự hiển thị, vui lòng kiểm tra lại", success = false });

            bool existCode = await db.SyllabusTemplateSections
                .AnyAsync(x => x.id_template == items.id_template &&
                               x.section_code.ToLower().Trim() == items.section_code.ToLower().Trim());
            if (existCode)
                return Ok(new { message = "Bị trùng Số thứ tự mục chính, vui lòng kiểm tra lại", success = false });
            bool existName = await db.SyllabusTemplateSections
                .AnyAsync(x => x.id_template == items.id_template &&
                               x.section_name.ToLower().Trim() == items.section_name.ToLower().Trim());
            if (existName)
                return Ok(new { message = "Bị trùng Tên tiêu đề, vui lòng kiểm tra lại", success = false });
            var newRecord = new SyllabusTemplateSection
            {
                id_template = items.id_template,
                section_name = items.section_name.Trim(),
                section_code = items.section_code.Trim(),
                allow_input = items.allow_input,
                order_index = items.order_index,
                id_contentType = items.id_contentType,
                id_dataBinding = items.id_dataBinding
            };
            db.SyllabusTemplateSections.Add(newRecord);
            await db.SaveChangesAsync();
            return Ok(new { message = "Tạo mới Tiêu đề mẫu đề cương thành công", success = true });
        }
        [HttpPost]
        [Route("loads-template-section")]
        public async Task<IActionResult> LoadTemplateSection([FromBody] SyllabusTemplateSectionDTOs items)
        {
            if (items.id_template == 0)
                return Ok(new { message = "Không tìm thấy thông tin mẫu đề cương", success = false });

            var loadItems = await db.SyllabusTemplateSections
                .Where(x => x.id_template == items.id_template)
                .OrderBy(x => x.order_index)
                .Select(x => new
                {
                    x.id_template_section,
                    x.section_code,
                    x.section_name,
                    x.order_index,
                    allow_input = x.allow_input == 0 ? "Không cho phép nhập liệu" : "Cho phép nhập liệu",
                    contentType = x.id_contentTypeNavigation.code + " - " + x.id_contentTypeNavigation.name,
                    dataBinding = x.id_dataBindingNavigation.code + " - " + x.id_dataBindingNavigation.name,
                })
                .ToListAsync();

            if (loadItems.Count == 0)
                return Ok(new { message = "Không có dữ liệu mẫu tiêu đề trong mẫu đề cương này", success = false });

            return Ok(new { data = loadItems, message = "Load biểu mẫu thành công", success = true });
        }
        [HttpPost]
        [Route("info-template-section")]
        public async Task<IActionResult> LoadInfoSectionTemplate([FromBody] SyllabusTemplateSectionDTOs items)
        {
            var checkSectionTemplate = await db.SyllabusTemplateSections
                .Where(x => x.id_template_section == items.id_template_section)
                .Select(x => new
                {
                    x.id_template_section,
                    x.id_template,
                    x.section_code,
                    x.section_name,
                    x.allow_input,
                    x.order_index,
                    x.id_dataBinding,
                    x.id_contentType
                })
                .FirstOrDefaultAsync();
            if (checkSectionTemplate == null)
                return Ok(new { message = "Không tìm thấy Câu hỏi tiêu đề", success = false });
            return Ok(new { data = checkSectionTemplate, success = true });
        }
        [HttpPost]
        [Route("update-template-section")]
        public async Task<IActionResult> EditTemplateSection([FromBody] SyllabusTemplateSectionDTOs items)
        {
            if (string.IsNullOrWhiteSpace(items.section_code))
                return Ok(new { message = "Không được bỏ trống trường Số thứ tự mục chính", success = false });

            if (string.IsNullOrWhiteSpace(items.section_name))
                return Ok(new { message = "Không được bỏ trống trường Tên tiêu đề", success = false });
            var checkSectionTemplate = await db.SyllabusTemplateSections.Where(x => x.id_template_section == items.id_template_section).FirstOrDefaultAsync();
            if (checkSectionTemplate == null)
                return Ok(new { message = "Không tìm thấy Câu hỏi tiêu đề", success = false });
            checkSectionTemplate.section_name = items.section_name.Trim();
            checkSectionTemplate.section_code = items.section_code.Trim();
            checkSectionTemplate.allow_input = items.allow_input;
            checkSectionTemplate.id_contentType = items.id_contentType == 0 ? null : items.id_contentType;
            checkSectionTemplate.id_dataBinding = items.id_dataBinding == 0 ? null : items.id_dataBinding;

            var list = await db.SyllabusTemplateSections
                .Where(x => x.id_template == checkSectionTemplate.id_template)
                .OrderBy(x => x.order_index)
                .ToListAsync();

            list.Remove(checkSectionTemplate);

            int newIndex = Math.Max(1, Math.Min((int)items.order_index, list.Count + 1));
            list.Insert(newIndex - 1, checkSectionTemplate);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].order_index = i + 1;
            }
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công và đã sắp xếp lại thứ tự hiển thị", success = true });
        }
        [HttpPost]
        [Route("delete-template-section")]
        public async Task<IActionResult> DeleteTemplateSection([FromBody] SyllabusTemplateSectionDTOs items)
        {
            var checkSectionTemplate = await db.SyllabusTemplateSections.Where(x => x.id_template_section == items.id_template_section).FirstOrDefaultAsync();
            if (checkSectionTemplate == null)
                return Ok(new { message = "Không tìm thấy Câu hỏi tiêu đề", success = false });
            db.SyllabusTemplateSections.Remove(checkSectionTemplate);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        [HttpPost]
        [Route("save-template")]
        public async Task<IActionResult> SaveTemplate([FromBody] TemplateSyllabusDTOs items)
        {
            var checkTemplate = await db.SyllabusTemplates.Where(x => x.id_template == items.id_template).FirstOrDefaultAsync();
            if (checkTemplate == null)
                return Ok(new { message = "Không tìm thấy thông tin biểu mẫu", success = false });
            checkTemplate.template_json = items.template_json;
            checkTemplate.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Lưu dữ liệu biểu mẫu thành công", success = true });
        }
        [HttpPost]
        [Route("preview-template")]
        public async Task<IActionResult> PreviewTemplate([FromBody] TemplateSyllabusDTOs items)
        {
            var checkTemplate = await db.SyllabusTemplates
                .Where(x => x.id_template == items.id_template)
                .Select(x => new
                {
                    x.id_template,
                    x.template_name,
                    x.template_json
                })
                .FirstOrDefaultAsync();
            if (checkTemplate == null)
                return Ok(new { message = "Không tìm thấy thông tin biểu mẫu", success = false });

            return Ok(new { data = checkTemplate, success = true });
        }
        [HttpGet]
        [Route("loads-selected-program")]
        public async Task<IActionResult> LoadsCTDTByDV()
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var GetListCTDT = await db.TrainingPrograms
                .Where(x => GetFaculty.Contains(x.id_faculty ?? 0))
                .Select(x => new
                {
                    x.id_program,
                    x.name_program
                })
                .ToListAsync();
            return Ok(GetListCTDT);
        }

        [HttpGet]
        [Route("loads-plo-hoc-phan")]
        public async Task<IActionResult> LoadPloHP()
        {
            var checkCourse = await db.Courses
                .Where(x => x.id_course == 84)
                .FirstOrDefaultAsync();

            if (checkCourse == null)
                return Ok(new { success = false, message = "Không tìm thấy học phần" });

            var listPlo = await db.ProgramLearningOutcomes
                .Where(x => x.Id_Program == checkCourse.id_program)
                .Select(x => new { x.Id_Plo, x.code })
                .ToListAsync();

            var mappedPloIds = await db.ContributionMatrices
                .Where(cm => cm.Id_PINavigation.Id_PLONavigation.Id_Program == checkCourse.id_program)
                .Select(cm => cm.Id_PINavigation.Id_PLO)
                .Distinct()
                .ToListAsync();

            var totalPloMapped = mappedPloIds.Count;

            var listData = new List<object>();

            foreach (var plo in listPlo)
            {
                if (!mappedPloIds.Contains(plo.Id_Plo)) continue; 

                var piList = await db.ContributionMatrices
                    .Where(cm => cm.Id_PINavigation.Id_PLO == plo.Id_Plo)
                    .Select(cm => new
                    {
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
    }
}
