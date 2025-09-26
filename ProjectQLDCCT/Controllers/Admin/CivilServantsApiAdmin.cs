using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
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
        [Route("add-new")]
        public async Task<IActionResult> AddNew([FromBody] CivilServantsDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
                return BadRequest(new { message = "Không được bỏ trống trường Mã viên chức", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return BadRequest(new { message = "Không được bỏ trống trường Tên viên chức", success = false });

            var new_record = new CivilServant
            {
                code_civilSer = items.code_civilSer,
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                birthday = items.birthday,
                id_year = items.value_year,
                time_cre = unixTimestamp,
                time_up = unixTimestamp
            };
            db.CivilServants.Add(new_record);
            await db.SaveChangesAsync();
            return Ok();
        }
        [HttpGet]
        [Route("load-thong-tin/{id}")]
        public async Task<IActionResult> LoadInfo(int id)
        {
            var GetItems = await db.CivilServants
                .Where(x => x.id_civilSer == id)
                .Select(x => new
                {
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    x.id_year
                })
                .FirstOrDefaultAsync();
            return Ok(GetItems);
        }
        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CivilServantsDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
                return BadRequest(new { message = "Không được bỏ trống trường Mã viên chức", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return BadRequest(new { message = "Không được bỏ trống trường Tên viên chức", success = false });
            var GetItems = await db.CivilServants.FirstOrDefaultAsync(x => x.id_civilSer == id);
            GetItems.code_civilSer = items.code_civilSer;
            GetItems.fullname_civilSer = items.fullname_civilSer;
            GetItems.email = items.email;
            GetItems.id_year = items.value_year;
            GetItems.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
    }
}
