using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("MappingCLOBySyllabus")]
public partial class MappingCLOBySyllabus
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string? map_clo { get; set; }

    public string? description { get; set; }

    public int? id_syllabus { get; set; }

    [InverseProperty("id_CLoMappingNavigation")]
    public virtual ICollection<MappingCLObyPI> MappingCLObyPIs { get; set; } = new List<MappingCLObyPI>();

    [ForeignKey("id_syllabus")]
    [InverseProperty("MappingCLOBySyllabi")]
    public virtual Syllabus? id_syllabusNavigation { get; set; }
}
