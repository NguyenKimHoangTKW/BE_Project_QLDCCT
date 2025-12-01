using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class NotificationDTOs
    {
        public int? id_notification { get; set; }

        public int? id_user { get; set; }

        public int? id_program { get; set; }

        public string? title { get; set; }

        public string? message { get; set; }

        public string? type { get; set; }

        public int? create_time { get; set; }

        public bool? is_read { get; set; }

        public string? link { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
