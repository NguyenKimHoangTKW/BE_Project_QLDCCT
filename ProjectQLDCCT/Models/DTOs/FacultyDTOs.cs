using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class FacultyDTOs
    {
        public int? id_faculty { get; set; }

        public string? code_faciulty { get; set; }

        public string? name_faculty { get; set; }

        public int? id_year { get; set; }
       
        public int? id_users { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
