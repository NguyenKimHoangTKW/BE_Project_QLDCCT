namespace ProjectQLDCCT.Models.DTOs
{
    public class UsersDTOs
    {
        public int? id_users { get; set; }
        public string? Username { get; set; }
        public string? email { get; set; }
        public string? avatar_url { get; set; }
        public int? id_type_users { get; set; }
        public int? status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
