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
        private readonly int unixTimestamp;

        public YearController(QLDCContext context)
        {
            _context = context;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var years = await _context.Years
                .Select(x => new { x.id_year, x.name_year })
                .ToListAsync();

            if (years.Any())
                return Ok(new { data = years, success = true });

            return Ok(new { message = "Chưa có dữ liệu", success = false });
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var year = await _context.Years
                .Where(x => x.id_year == id)
                .Select(x => new { value_year = x.id_year, name_year = x.name_year })
                .FirstOrDefaultAsync();

            if (year == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu", success = false });

            return Ok(new { data = year, success = true });
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] YearsDTO items)
        {
            if (string.IsNullOrWhiteSpace(items.name_year))
                return BadRequest(new { message = "Không được bỏ trống Tên năm học", success = false });

            var newYear = new Year
            {
                name_year = items.name_year,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };

            _context.Years.Add(newYear);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thêm dữ liệu thành công", success = true });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] YearsDTO items)
        {
            if (string.IsNullOrWhiteSpace(items.name_year))
                return BadRequest(new { message = "Không được bỏ trống Tên năm học", success = false });

            var year = await _context.Years.FirstOrDefaultAsync(x => x.id_year == id);
            if (year == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu", success = false });

            year.name_year = items.name_year;
            year.time_up = unixTimestamp;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Chỉnh sửa dữ liệu thành công", success = true });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var year = await _context.Years.FirstOrDefaultAsync(x => x.id_year == id);
            if (year == null)
                return NotFound(new { message = "Không tìm thấy dữ liệu", success = false });

            _context.Years.Remove(year);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
    }
}
