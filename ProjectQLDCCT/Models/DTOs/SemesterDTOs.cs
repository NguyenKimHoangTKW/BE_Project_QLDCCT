using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class SemesterDTOs
    {
        public int id_semester { get; set; }
        public string? code_semester { get; set; }
        public string? name_semester { get; set; }

        public int? id_faculty { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? searchTerm { get; set; }
    }
}
