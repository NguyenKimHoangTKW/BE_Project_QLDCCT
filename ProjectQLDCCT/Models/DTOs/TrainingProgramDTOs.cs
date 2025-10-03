using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class TrainingProgramDTOs
    {
        public int? id_program { get; set; }

        public int? id_faculty { get; set; }

        public string? code_program { get; set; }

        public string? name_program { get; set; }

        public int? time_cre { get; set; }

        public int? time_up { get; set; }
        public int? id_year { get; set; }
    }
}
