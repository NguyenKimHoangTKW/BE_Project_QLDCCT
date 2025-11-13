using System.ComponentModel.DataAnnotations;

namespace ProjectQLDCCT.Models.DTOs
{
    public class MappingCLOBySyllabusDTOs
    {
        public int? id { get; set; }

        public string? map_clo { get; set; }

        public string? description { get; set; }

        public int? id_syllabus { get; set; }
    }
}
