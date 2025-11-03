using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CLO_PLO_Mapping")]
public partial class CLO_PLO_Mapping
{
    [Key]
    public int id { get; set; }

    public int id_CLO { get; set; }

    public int id_CoreCourseMatrix { get; set; }

    public int? id_program { get; set; }

    public int? Id_Level { get; set; }

    [StringLength(255)]
    public string? Comment { get; set; }

    [ForeignKey("Id_Level")]
    [InverseProperty("CLO_PLO_Mappings")]
    public virtual LevelContribution? Id_LevelNavigation { get; set; }

    [ForeignKey("id_CLO")]
    [InverseProperty("CLO_PLO_Mappings")]
    public virtual CourseLearningOutcome id_CLONavigation { get; set; } = null!;

    [ForeignKey("id_CoreCourseMatrix")]
    [InverseProperty("CLO_PLO_Mappings")]
    public virtual CoreCourseMatrix id_CoreCourseMatrixNavigation { get; set; } = null!;

    [ForeignKey("id_program")]
    [InverseProperty("CLO_PLO_Mappings")]
    public virtual TrainingProgram? id_programNavigation { get; set; }
}
