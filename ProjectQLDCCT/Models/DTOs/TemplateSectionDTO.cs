namespace ProjectQLDCCT.Models.DTOs
{
    public class TemplateSectionDTO
    {
        public int id_template_section { get; set; }
        public string section_code { get; set; }
        public string section_name { get; set; }
        public string allow_input { get; set; }
        public string contentType { get; set; }
        public string dataBinding { get; set; }
        public string value { get; set; }
    }
}
