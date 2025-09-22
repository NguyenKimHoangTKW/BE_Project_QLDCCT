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
        [Route("civil-servants")]
        public async Task<IActionResult> GetALlTest()
        {
            var civil = await _context.CivilServants
                .Select(x => new
                {
                    x.id_yearNavigation.name_year,
                    x.code_civilSer
                })
                .ToListAsync();
            return Ok(civil);
        }
        [HttpPost]
        [Route("add-civil-servants")]
        public async Task<IActionResult> AddNewCivils([FromBody] CivilServant items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
            {
                return BadRequest("Không được bỏ trống Mã viên chức");
            }
            if (string.IsNullOrEmpty(items.fullname_civilSer))
            {
                return BadRequest("Không được bỏ trống Tên viên chức");
            }
            var new_record = new CivilServant
            {
                code_civilSer = items.code_civilSer,
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                id_year = items.id_year,
                time_cre = unixTimestamp,
                time_up = unixTimestamp
            };
            _context.CivilServants.Add(new_record);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công" });
        }
    }
}
