using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class StudentDTOs
    {
        public int? id_student { get; set; }

        public string? code_student { get; set; }

        public string? name_student { get; set; }

        public int? id_class { get; set; }
        public int? id_program { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
