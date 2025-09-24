using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.Threading.Tasks;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/year")]
    [ApiController]
    public class YearController : ControllerBase
    {
        private readonly QLDCContext _context;
        private int unixTimestamp;
        public YearController(QLDCContext context)
        {
            _context = context;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpGet]
        [Route("load-danh-sach-nam")]
        public async Task<IActionResult> GetALlTest()
        {
            var civil = await _context.Years
                .Select(x => new
                {
                    x.id_year,
                    x.name_year
                })
                .ToListAsync();
            if (civil.Count > 0)
            {
                return Ok(new { data = civil, success = true });
            }
            else
            {
                return Ok(new { message = "Chưa có dữ liệu", success = false });
            }

        }
        [HttpPost]
        [Route("them-moi-nam-hoc")]
        public async Task<IActionResult> ThemMoiNam([FromBody] YearsDTO items)
        {
            if (string.IsNullOrEmpty(items.name_year))
            {
                return Ok(new { message = "Không được bỏ trống Tên năm học", success = false });
            }
            var new_record = new Year
            {
                name_year = items.name_year,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            _context.Years.Add(new_record);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("load-thong-tin-nam-hoc")]
        public async Task<IActionResult> LoadInfo([FromBody] YearsDTO items)
        {
            var GetData = await _context.Years
                .Where(x => x.id_year == items.value_year)
                .Select(x => new
                {
                    value_year = x.id_year,
                    name_year = x.name_year
                })
                .FirstOrDefaultAsync();
            return Ok(GetData);
        }
        [HttpPost]
        [Route("cap-nhat-nam-hoc")]
        public async Task<IActionResult> CapNhatNamHoc([FromBody] YearsDTO items)
        {
            if (string.IsNullOrEmpty(items.name_year))
            {
                return Ok(new { message = "Không được bỏ trống Tên năm học", success = false });
            }
            var CheckItems = await _context.Years.FirstOrDefaultAsync(x => x.id_year == items.value_year);
            CheckItems.name_year = items.name_year;
            CheckItems.time_up = unixTimestamp;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Chỉnh sửa dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-nam-hoc")]
        public async Task<IActionResult> XoaNamHoc([FromBody] YearsDTO items)
        {
            return Ok();
        }

    }
}
