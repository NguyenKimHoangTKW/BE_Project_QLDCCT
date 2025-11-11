namespace ProjectQLDCCT.Models.DTOs
{
    public class OpenSyllabusWindowsCourseDTOs
    {
        public int? id { get; set; }

        public int? id_course { get; set; }

        public int? open_time { get; set; }

        public int? close_time { get; set; }

        public string? reason { get; set; }

        public int? created_by { get; set; }
        public int? id_keyYearSemester { get; set; }
        public int? id_program { get; set; }
        public int? is_open { get; set; }
    }
}
