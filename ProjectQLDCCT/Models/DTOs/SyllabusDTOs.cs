
using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class SyllabusDTOs
    {
        public int? id_syllabus { get; set; }

        public int? id_teacherbysubject { get; set; }

        public int? id_status { get; set; }

        public string? version { get; set; }
        public int? id_course { get; set; }
        public string? syllabus_json {  get; set; }

        public int? id_program { get; set; }
        public string? returned_content { get; set; }
        public string? edit_content { get; set; }

        public int? is_open_edit_final { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? searchTerm { get; set; }
    }
}
