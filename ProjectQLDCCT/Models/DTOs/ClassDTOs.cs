using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class ClassDTOs
    {
        public int? id_class { get; set; }

        public string? name_class { get; set; }

        public int? id_program { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? searchTerm { get; set; }
    }
}
