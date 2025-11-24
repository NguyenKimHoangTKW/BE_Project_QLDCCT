using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models;
using ProjectQLDCCT.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Authorize(Policy = "Admin")]
    [Route("api/admin/users")]
    [ApiController]
    public class ListUserApiAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public ListUserApiAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpGet]
        [Route("loads-select-type-users")]
        public async Task<IActionResult> LoadSelectedtypeUser()
        {
            var ListSelected = await db.TypeUsers
                .Select(x => new
                {
                    value = x.id_type_users,
                    name = x.name_type_users,
                })
                .ToListAsync();
            if (ListSelected.Count > 0)
            {
                return Ok(new { data = ListSelected, success = true });
            }
            return Ok(new { message = "Không có dữ liệu", success = false });
        }
        [HttpPost]
        [Route("loads-danh-sach-users/{idtype}")]
        public async Task<IActionResult> DanhSachUser(int idtype, [FromBody] DataTableRequest request)
        {
            var query = db.Users.AsQueryable();
            if (idtype != 0)
            {
                query = query.Where(x => x.id_type_users == idtype);
            }
            var _query = query
                .Select(x => new
                {
                    x.id_users,
                    x.Username,
                    x.email,
                    x.time_cre,
                    x.time_up,
                    x.avatar_url,
                    x.id_type_usersNavigation.name_type_users,
                    status = x.status == 1 ? "Đang hoạt động" : "Đã khóa"
                });
            var result = await DataTableHelper.GetDataTableAsync(_query, request);
            return Ok(result);
        }
        [HttpPost]
        [Route("them-moi-thu-cong-user")]
        public async Task<IActionResult> AddNewUser([FromBody] UsersDTOs items)
        {
            if (string.IsNullOrEmpty(items.email))
                return Ok(new { message = "Không được bỏ trống Email", success = false });
            var CheckEmail = await db.Users.FirstOrDefaultAsync(x => x.email == items.email);
            if (CheckEmail != null)
                return Ok(new { message = "Email này đã tồn tại, vui lòng kiểm tra lại", success = false });
            var new_record = new User
            {
                email = items.email,
                status = 1,
                id_type_users = 1,
                time_cre = unixTimestamp,
                time_up = unixTimestamp,
            };
            db.Users.Add(new_record);
            await db.SaveChangesAsync();
            return Ok(new { message = "Thêm mới dữ liệu thành công", success = true });
        }
        [HttpDelete]
        [Route("xoa-users/{id}")]
        public async Task<IActionResult> DeleteUserr(int id)
        {
            var CheckUser = await db.Users.FirstOrDefaultAsync(x => x.id_users == id);
            if (CheckUser == null)
            {
                return Ok(new { message = "Không có dữ liệu users", success = false });

            }
            db.Users.Remove(CheckUser);
            await db.SaveChangesAsync();
            return Ok(new { message = "Xóa dữ liệu thành công", success = true });
        }
        [HttpPost]
        [Route("info-user")]
        public async Task<IActionResult> LoadInfo([FromBody] UsersDTOs items)
        {
            var CheckItems = await db.Users
                .Where(x => x.id_users == items.id_users)
                .Select(x => new
                {
                    x.id_users,
                    x.id_type_users,
                    x.email,
                    x.avatar_url,
                    x.id_type_usersNavigation.name_type_users
                })
                .FirstOrDefaultAsync();
            return Ok(CheckItems);
        }
        [HttpPost]
        [Route("loads-danh-sach-don-vi")]
        public async Task<IActionResult> LoadListDonVi([FromBody] FacultyDTOs items)
        {
            var ListDonVi = await db.Faculties
                .Select(x => new
                {
                    x.id_faculty,
                    x.name_faculty
                })
                .ToListAsync();

            var userFaculties = await db.UserByFaculPrograms
                .Where(x => x.id_users == items.id_users && x.id_faculty != null)
                .Select(x => x.id_faculty)
                .ToListAsync();
            var result = ListDonVi.Select(x => new
            {
                x.id_faculty,
                x.name_faculty,
                checkedPermission = userFaculties.Contains(x.id_faculty)
            });
            return Ok(result);
        }
        [HttpPost]
        [Route("loads-danh-sach-ctdt")]
        public async Task<IActionResult> LoadListCTDT([FromBody] FacultyDTOs items)
        {
            var ListCTDT = await db.TrainingPrograms
                .Select(x => new
                {
                    x.id_program,
                    x.name_program
                })
                .ToListAsync();
            var userPrograming = await db.UserByFaculPrograms
               .Where(x => x.id_users == items.id_users && x.id_program != null)
               .Select(x => x.id_program)
               .ToListAsync();
            var result = ListCTDT.Select(x => new
            {
                x.id_program,
                x.name_program,
                checkedPermission = userPrograming.Contains(x.id_program)
            });
            return Ok(result);
        }
        public class SavePermissionDTOs
        {
            public int? id_user { get; set; }
            public List<int?> id_FacPro { get; set; }
            public string name_permission { get; set; }
            public int? id_type_users { get; set; }
        };
        [HttpPost]
        [Route("save-quyen-users")]
        public async Task<IActionResult> SavePermission([FromBody] SavePermissionDTOs items)
        {
            var checkuser = await db.Users.FirstOrDefaultAsync(x => x.id_users == items.id_user);
            checkuser.id_type_users = items.id_type_users;
            var oldRecords = db.UserByFaculPrograms.Where(x => x.id_users == items.id_user);
            db.UserByFaculPrograms.RemoveRange(oldRecords);
            await db.SaveChangesAsync();
            if (items.name_permission == "Đơn vị")
            {
                var checkDonVi = await db.Faculties
                    .Where(x => items.id_FacPro.Contains(x.id_faculty))
                    .Select(x => x.id_faculty)
                    .ToListAsync();
                foreach (var idFac in checkDonVi)
                {
                    var newRecord = new UserByFaculProgram
                    {
                        id_users = items.id_user,
                        id_program = null,
                        id_faculty = idFac
                    };
                    db.UserByFaculPrograms.Add(newRecord);
                }
                await db.SaveChangesAsync();
            }
            else if (items.name_permission == "CTĐT")
            {
                var CheckCTDT = await db.TrainingPrograms
                   .Where(x => items.id_FacPro.Contains(x.id_program))
                   .Select(x => x.id_program)
                   .ToListAsync();
                foreach (var idPro in CheckCTDT)
                {
                    var newRecord = new UserByFaculProgram
                    {
                        id_users = items.id_user,
                        id_program = idPro,
                        id_faculty = null
                    };
                    db.UserByFaculPrograms.Add(newRecord);
                }
                await db.SaveChangesAsync();
            }

            return Ok(new { message = "Lưu quyền thành công", success = true });
        }
    }
}
