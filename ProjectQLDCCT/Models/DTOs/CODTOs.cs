using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class CODTOs
    {
        public int id { get; set; }
        public int? id_program { get; set; }
        public string? name_CO { get; set; }

        public string? describe_CO { get; set; }

        public string? typeOfCapacity { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
