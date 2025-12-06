using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class CourseDTOs
    {

        public int id_course { get; set; }

        public int? id_program { get; set; }

        public string? code_course { get; set; }

        public string? name_course { get; set; }

        public int? id_gr_course { get; set; }

        public int? id_semester { get; set; }

        public int? credits { get; set; }
        public int? id_isCourse { get; set; }
        public int? totalTheory { get; set; }
        public int? id_key_year_semester { get; set; }

        public int? totalPractice { get; set; }
        public string? searchTerm { get; set; }

        public int? is_obligatory { get; set; }

        public int? is_selfselect { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
