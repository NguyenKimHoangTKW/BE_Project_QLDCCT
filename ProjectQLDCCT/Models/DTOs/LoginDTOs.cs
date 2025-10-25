using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class LoginDTOs
    {
        public int id_users { get; set; }
        public string? Username { get; set; }
        public string? email { get; set; }
        public int? time_cre { get; set; }
        public int? time_up { get; set; }
        public string? avatar_url { get; set; }
        public int? id_type_users { get; set; }
        public int status { get; set; }
    }
}
