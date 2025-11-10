using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.CTDT
{
    [Route("api/ctdt/civil-servants")]
    [ApiController]
    public class CivilServantsCTDTAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public CivilServantsCTDTAPI(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        [HttpPost]
        [Route("loads-danh-sach-can-bo-vien-chuc")]
        public async Task<IActionResult> LoadCBVC([FromBody] CivilServantsDTOs items)
        {
            var excludeIds = new int?[] { 2, 3 };

            var query = from cs in db.CivilServants
                        join u in db.Users on cs.email equals u.email into csu
                        from user in csu.DefaultIfEmpty()
                        where cs.id_program == items.id_program
                              && (
                                  user == null
                                  || !excludeIds.Contains(user.id_type_users)
                              )
                        orderby cs.id_civilSer descending
                        select new
                        {
                            cs.id_civilSer,
                            cs.code_civilSer,
                            cs.fullname_civilSer,
                            cs.email,
                            cs.birthday,
                            ProgramName = cs.id_programNavigation.name_program,
                            cs.time_up,
                            cs.time_cre
                        };

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data,
                currentPage = items.Page,
                items.PageSize,
                totalRecords,
                totalPages = (int)Math.Ceiling(totalRecords / (double)items.PageSize)
            });
        }
        [HttpPost]
        [Route("them-moi-can-bo-vien-chuc")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });
            var CheckNameCV = await db.CivilServants
                .Where(x => items.id_program == x.id_program && x.code_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim() && x.fullname_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckNameCV != null)
                return Ok(new { message = "Cán bộ viên chức này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var CheckEmailCV = await db.CivilServants
                .Where(x => items.id_program == x.id_program && x.email.ToLower().Trim() == items.email.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckEmailCV != null)
                return Ok(new { message = "Email này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var new_record = new CivilServant
            {
                fullname_civilSer = items.fullname_civilSer,
                email = items.email,
                code_civilSer = items.code_civilSer,
                birthday = items.birthday,
                id_program = items.id_program,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.CivilServants.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-can-bo-vien-chuc")]
        public async Task<IActionResult> InfoCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
                .Where(x => x.id_civilSer == items.id_civilSer)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.email,
                    x.code_civilSer,
                    x.birthday,
                    x.fullname_civilSer,
                    x.id_program
                })
                .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            return Ok(new { data = checkInfo, success = true });
        }
        [HttpPost]
        [Route("update-can-bo-vien-chuc")]
        public async Task<IActionResult> UpdateCO([FromBody] CivilServantsDTOs items)
        {

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });

            var checkInfo = await db.CivilServants
              .Where(x => x.id_civilSer == items.id_civilSer)
              .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            checkInfo.fullname_civilSer = items.fullname_civilSer;
            checkInfo.code_civilSer = items.code_civilSer;
            checkInfo.email = items.email;
            checkInfo.birthday = items.birthday;
            checkInfo.id_program = items.id_program;
            checkInfo.time_up = unixTimestamp;
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật thông tin thành công", success = true });
        }
        [HttpPost]
        [Route("xoa-du-lieu-can-bo-vien-chuc")]
        public async Task<IActionResult> DeleteCO([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
            .Where(x => x.id_civilSer == items.id_civilSer)
            .FirstOrDefaultAsync();
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thầy thông tin Cán bộ viên chức", success = false });
            db.CivilServants.Remove(checkInfo);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }

        
    }
}
