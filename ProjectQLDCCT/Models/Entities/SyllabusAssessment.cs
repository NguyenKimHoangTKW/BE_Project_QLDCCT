using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusAssessment")]
public partial class SyllabusAssessment
{
    [Key]
    public int id_assessment { get; set; }

    public int id_syllabus { get; set; }

    [StringLength(200)]
    public string method_name { get; set; } = null!;

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? weight { get; set; }

    [InverseProperty("id_assessmentNavigation")]
    public virtual ICollection<AssessmentCLO> AssessmentCLOs { get; set; } = new List<AssessmentCLO>();

    [ForeignKey("id_syllabus")]
    [InverseProperty("SyllabusAssessments")]
    public virtual Syllabus id_syllabusNavigation { get; set; } = null!;
}
