namespace ProjectQLDCCT.Models.DTOs
{
    public class CivilServantsDTOs
    {
        public int? id_civilSer { get; set; }
        public string? code_civilSer { get; set; }
        public string? fullname_civilSer { get; set; }
        public string? email { get; set; }
        public DateOnly? birthday { get; set; }
        public int? value_year { get; set; }
        public int? id_program { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? id_type_users { get; set; }
        public int status { get; set; }
        public int? id_course { get; set; }
        public int? id_syllabus { get; set; }
        public List<int?>? ctdt_per { get; set; } = new();
    }
}
