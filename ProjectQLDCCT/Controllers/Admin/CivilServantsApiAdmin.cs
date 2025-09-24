using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/civilservants")]
    [ApiController]
    public class CivilServantsApiAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CivilServantsApiAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpPost]
        public async Task<IActionResult> GetCivilServants([FromBody] DataTableRequest request)
        {
            var query = db.CivilServants
                .Select(x => new
                {
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    name_year = x.id_yearNavigation.name_year,
                    x.time_cre,
                    x.time_up
                });

            var result = await DataTableHelper.GetDataTableAsync(query, request,
                x => x.fullname_civilSer,
                x => x.email,
                x => x.code_civilSer
            );
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> AddNew([FromBody] CivilServantsDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
                return BadRequest(new { message = "Không được bỏ trống trường Mã viên chức", success = false });

            return Ok();
        }
    }
}
