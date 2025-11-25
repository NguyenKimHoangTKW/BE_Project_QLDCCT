namespace ProjectQLDCCT.Models.DTOs
{
    public class SaveSyllabusDraftContentDTO
    {
        public int? id { get; set; }

        public int? id_syllabus { get; set; }

        public string? draft_json { get; set; }
    }
}
