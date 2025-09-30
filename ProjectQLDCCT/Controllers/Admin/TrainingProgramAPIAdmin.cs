using Azure.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectQLDCCT.Data;
using ProjectQLDCCT.Helpers;
using ProjectQLDCCT.Models.DTOs;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/program")]
    [ApiController]
    public class TrainingProgramAPIAdmin : ControllerBase
    {
        private readonly QLDCContext db;
        private readonly int unixTimestamp;
        public TrainingProgramAPIAdmin(QLDCContext _db)
        {
            db = _db;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        [HttpGet]
        [Route("loads-select-don-vi-by-ctdt")]
        public async Task<IActionResult> LoadDonViByCTDT()
        {
            var GetDV = await db.Faculties
                .Select(x => new
                {
                   value = x.id_faculty,
                   name = x.name_faculty
                })
                .ToListAsync();
            return Ok(GetDV);
        }
        [HttpPost]
        [Route("loads-ctdt-by-don-vi/{idDonvi}")]
        public async Task<IActionResult> LoadData(int idDonvi,[FromBody] DataTableRequest items)
        {
            var query = db.TrainingPrograms
                .Where(x => x.id_faculty == idDonvi)
                .Select(x => new
                {
                    x.id_program,
                    x.code_program,
                    x.name_program,
                    x.time_up,
                    x.time_cre,
                    x.id_facultyNavigation.name_faculty
                });
            var result = await DataTableHelper.GetDataTableAsync(query, items,
                x => x.code_program,
                x => x.name_program
                );
            return Ok(result);
        }
    }
}
