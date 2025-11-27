using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class PLODTOs
    {
        public int Id_Plo { get; set; }

        public int? Id_Program { get; set; }
        public int? id_key_semester { get; set; }
        public string? code { get; set; }

        public int? order_index { get; set; }
        public string? Description { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public int? id_syllabus { get; set; }
    }
}
