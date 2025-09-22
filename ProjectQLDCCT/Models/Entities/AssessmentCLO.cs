using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("AssessmentCLO")]
public partial class AssessmentCLO
{
    [Key]
    public int id_assess_clo { get; set; }

    public int id_clo { get; set; }

    public int id_assessment { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? weight { get; set; }

    [ForeignKey("id_assessment")]
    [InverseProperty("AssessmentCLOs")]
    public virtual SyllabusAssessment id_assessmentNavigation { get; set; } = null!;

    [ForeignKey("id_clo")]
    [InverseProperty("AssessmentCLOs")]
    public virtual CLO id_cloNavigation { get; set; } = null!;
}
