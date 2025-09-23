using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using System.Threading.Tasks;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin")]
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
    }
}
