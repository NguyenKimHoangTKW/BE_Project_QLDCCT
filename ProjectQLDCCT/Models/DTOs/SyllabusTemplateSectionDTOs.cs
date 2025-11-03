using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class SyllabusTemplateSectionDTOs
    {
        public int id_template_section { get; set; }

        public int id_template { get; set; }

        public string section_code { get; set; } = null!;

        public string section_name { get; set; } = null!;

        public int? is_required { get; set; }

        public int? order_index { get; set; }

        public int? id_contentType { get; set; }

        public int? id_dataBinding { get; set; }
    }
}
