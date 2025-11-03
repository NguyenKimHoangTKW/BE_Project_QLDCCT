using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class PIDTOs
    {
        public int Id_PI { get; set; }

        public int? Id_PLO { get; set; }
        public string? code { get; set; }

        public string? Description { get; set; }
        public int? order_index { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
