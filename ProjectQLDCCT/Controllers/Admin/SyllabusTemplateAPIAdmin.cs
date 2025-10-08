using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectQLDCCT.Data;

namespace ProjectQLDCCT.Controllers.Admin
{
    [Route("api/admin/year")]
    [ApiController]
    public class SyllabusTemplateAPIAdmin : ControllerBase
    {
        private readonly QLDCContext _context;
        private readonly int unixTimestamp;

        public SyllabusTemplateAPIAdmin(QLDCContext context)
        {
            _context = context;
            DateTime now = DateTime.UtcNow;
            unixTimestamp = (int)(now.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
