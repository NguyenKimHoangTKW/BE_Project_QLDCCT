using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CLO")]
public partial class CLO
{
    [Key]
    public int id_clo { get; set; }

    public int id_syllabus { get; set; }

    [StringLength(50)]
    public string clo_code { get; set; } = null!;

    public string? clo_description { get; set; }

    [InverseProperty("id_cloNavigation")]
    public virtual ICollection<AssessmentCLO> AssessmentCLOs { get; set; } = new List<AssessmentCLO>();

    [InverseProperty("id_cloNavigation")]
    public virtual ICollection<CLOStatistic> CLOStatistics { get; set; } = new List<CLOStatistic>();

    [ForeignKey("id_syllabus")]
    [InverseProperty("CLOs")]
    public virtual Syllabus id_syllabusNavigation { get; set; } = null!;
}
