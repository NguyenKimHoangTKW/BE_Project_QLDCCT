using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class KeySemesterDTOs
    {
        public int id_key_year_semester { get; set; }

        public string? code_key_year_semester { get; set; }

        public string? name_key_year_semester { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchText { get; set; }
        public int? id_faculty { get; set; }
    }
}
