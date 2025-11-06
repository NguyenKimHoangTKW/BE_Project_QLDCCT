using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.DonVi
{
    [Route("api/donvi/civil-servants")]
    [ApiController]
    public class CivilServantsDVAPI : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        private List<int> GetFaculty = new List<int>();
        public CivilServantsDVAPI(QLDCContext _db)
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
        [HttpGet]
        [Route("loads-type-permission")]
        public async Task<IActionResult> LoadTypePermission()
        {
            var CheckListType = new int?[] { 1, 2 };
            var GetList = await db.TypeUsers
                .Where(x => CheckListType.Contains(x.id_type_users))
                .Select(x => new
                {
                    x.id_type_users,
                    x.name_type_users
                })
                .ToListAsync();
            return Ok(GetList);
        }
        [HttpGet]
        [Route("loads-ctdt-by-don-vi")]
        public async Task<IActionResult> LoadCTDTByFaculty()
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
        [HttpPost]
        [Route("loads-danh-sach-can-bo-vien-chuc")]
        public async Task<IActionResult> LoadCBVC([FromBody] CivilServantsDTOs items)
        {
            var GetFaculty = await GetUserPermissionFaculties();
            var totalRecords = await db.CivilServants
                .Where(x => GetFaculty.Contains(x.id_programNavigation.id_faculty ?? 0))
                .CountAsync();
            var query = db.CivilServants
                .Where(x => GetFaculty.Contains(x.id_programNavigation.id_faculty ?? 0)).AsQueryable();
            if (items.id_program > 0)
            {
                query = query.Where(x => x.id_program == items.id_program);
            }
            var GetItems = await query
                .OrderByDescending(x => x.id_civilSer)
                .Skip((items.Page - 1) * items.PageSize)
                .Take(items.PageSize)
                .Select(x => new
                {
                    x.id_civilSer,
                    x.code_civilSer,
                    x.fullname_civilSer,
                    x.email,
                    x.birthday,
                    x.id_programNavigation.name_program,
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
        [Route("them-moi-can-bo-vien-chuc")]
        public async Task<IActionResult> ThemMoiCO([FromBody] CivilServantsDTOs items)
        {
            var GetFaculty = await GetUserPermissionFaculties();

            if (string.IsNullOrEmpty(items.code_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Mã CBVC", success = false });
            if (string.IsNullOrEmpty(items.fullname_civilSer))
                return Ok(new { message = "Không được bỏ trống trường Tên CBVC", success = false });
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống trường Email CBVC", success = false });
            var CheckNameCV = await db.CivilServants
                .Where(x => GetFaculty.Contains(x.id_programNavigation.id_faculty ?? 0) && x.code_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim() && x.fullname_civilSer.ToLower().Trim() == items.code_civilSer.ToLower().Trim())
                .FirstOrDefaultAsync();
            if (CheckNameCV != null)
                return Ok(new { message = "Cán bộ viên chức này đã tồn tại, vui lòng kiểm tra lại", success = false });

            var CheckEmailCV = await db.CivilServants
                .Where(x => GetFaculty.Contains(x.id_programNavigation.id_faculty ?? 0) && x.email.ToLower().Trim() == items.email.ToLower().Trim())
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
            var GetFaculty = await GetUserPermissionFaculties();
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
            var GetFaculty = await GetUserPermissionFaculties();

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
        
        [HttpPost]
        [Route("save-permission-cbvc")]
        public async Task<IActionResult> SaveQuyen([FromBody] CivilServantsDTOs items)
        {
            var checkInfo = await db.CivilServants
                     .FirstOrDefaultAsync(x => x.id_civilSer == items.id_civilSer);
            if (checkInfo == null)
                return Ok(new { message = "Không tìm thấy thông tin Cán bộ viên chức", success = false });
            var CheckEmail = await db.Users.FirstOrDefaultAsync(x => x.email == checkInfo.email);
            if (CheckEmail == null)
            {
                var newUser = new User
                {
                    email = checkInfo.email,
                    time_cre = unixTimestamp,
                    time_up = unixTimestamp,
                    id_type_users = items.id_type_users,
                    status = 1
                };
                db.Users.Add(newUser);
                await db.SaveChangesAsync();
                CheckEmail = newUser;
            }
            else
            {
                CheckEmail.id_type_users = items.id_type_users;
                CheckEmail.status = items.status;
                CheckEmail.time_up = unixTimestamp;
                db.Users.Update(CheckEmail);
                await db.SaveChangesAsync();
            }
            var oldPrograms = await db.UserByFaculPrograms
                .Where(x => x.id_users == CheckEmail.id_users)
                .ToListAsync();

            if (oldPrograms.Any())
                db.UserByFaculPrograms.RemoveRange(oldPrograms);

            if (items.id_type_users == 2 && items.ctdt_per != null)
            {
                foreach (var it in items.ctdt_per)
                {
                    db.UserByFaculPrograms.Add(new UserByFaculProgram
                    {
                        id_users = CheckEmail.id_users,
                        id_program = it
                    });
                }
            }
            await db.SaveChangesAsync();
            return Ok(new { message = "Cập nhật quyền quản lý cho Cán bộ viên chức thành công", success = true });
        }
        [HttpPost]
        [Route("get-permission-cbvc")]
        public async Task<IActionResult> GetPermissionCBVC([FromBody] CivilServantsDTOs items)
        {
            try
            {
                var cbvc = await db.CivilServants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.id_civilSer == items.id_civilSer);

                if (cbvc == null)
                    return Ok(new { message = "Không tìm thấy thông tin Cán bộ viên chức", success = false });

                var user = await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.email == cbvc.email);

                if (user == null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Cán bộ viên chức chưa được cấp quyền",
                        data = new
                        {
                            id_civilSer = cbvc.id_civilSer,
                            full_name = cbvc.fullname_civilSer,
                            email = cbvc.email,
                            id_users = (int?)null,
                            id_type_users = (int?)null,
                            status = 0,
                            list_programs = new List<object>()
                        }
                    });
                }

                var programs = await (
                    from up in db.UserByFaculPrograms
                    join p in db.TrainingPrograms on up.id_program equals p.id_program
                    where up.id_users == user.id_users
                    select new
                    {
                        p.id_program,
                        p.name_program
                    }
                ).ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin quyền thành công",
                    data = new
                    {
                        id_civilSer = cbvc.id_civilSer,
                        full_name = cbvc.fullname_civilSer,
                        email = cbvc.email,
                        id_users = user.id_users,
                        id_type_users = user.id_type_users,
                        status = user.status,
                        list_programs = programs
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { message = "Lỗi khi tải dữ liệu quyền: " + ex.Message, success = false });
            }
        }

    }
}
