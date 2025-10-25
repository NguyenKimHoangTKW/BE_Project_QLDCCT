using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class TemplateSyllabusDTOs
    {
        public int id_template { get; set; }

        public string? template_name { get; set; }

        public int? is_default { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
