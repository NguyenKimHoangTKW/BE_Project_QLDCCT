using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Authorize(Policy = "Admin")]
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
        [Route("{id}")]
        public async Task<IActionResult> GetCivilServants(int id, [FromBody] DataTableRequest request)
        {
            var query = db.CivilServants
                .Select(x => new
                {
                    x.id_civilSer,
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    x.time_cre,
                    x.time_up
                });

            var result = await DataTableHelper.GetDataTableAsync(query, request);
            return Ok(result);
        }
        [HttpPost]
        [Route("add-new")]
        public async Task<IActionResult> AddNew([FromBody] CivilServantsDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã viên chức", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên viên chức", success = false });

            var new_record = new CivilServant
            {
                code_civilSer = items.code_civilSer,
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                birthday = items.birthday,
                time_cre = unixTimestamp,
                time_up = unixTimestamp
            };
            db.CivilServants.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpGet]
        [Route("load-thong-tin/{id}")]
        public async Task<IActionResult> LoadInfo(int id)
        {
            var GetItems = await db.CivilServants
                .Where(x => x.id_civilSer == id)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                })
                .FirstOrDefaultAsync();
            return Ok(GetItems);
        }
        [HttpPut]
        [Route("update/{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] CivilServantsDTOs items)
        {
            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã viên chức", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên viên chức", success = false });
            var GetItems = await db.CivilServants.FirstOrDefaultAsync(x => x.id_civilSer == id);
            GetItems.code_civilSer = items.code_civilSer;
            GetItems.fullname_civilSer = items.fullname_civilSer;
            GetItems.email = items.email;
            GetItems.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật dữ liệu thành công", success = true });
        }
        [HttpDelete]
        [Route("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var checkItems = await db.CivilServants
                .FirstOrDefaultAsync(x => x.id_civilSer == id);
            if (checkItems != null)
            {
                db.CivilServants.Remove(checkItems);
            }
            else
            {
                return Ok(new { message = "Không tìm thấy thông tin dữ liệu", success = false });
            }
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

    }
}
