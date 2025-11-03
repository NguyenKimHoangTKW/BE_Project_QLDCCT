using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class CLODTOs
    {
        public int id { get; set; }

        public string? name_CLO { get; set; }

        public string? describe_CLO { get; set; }

        public int? id_faculty { get; set; }

        public int? program_id { get; set; }

        public string? bloom_level { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
