
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
    }
}
