using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CLO_CO_Mapping")]
public partial class CLO_CO_Mapping
{
    [Key]
    public int id { get; set; }

    public int id_CLO { get; set; }

    public int id_CO { get; set; }

    public int? id_program { get; set; }

    [ForeignKey("id_CLO")]
    [InverseProperty("CLO_CO_Mappings")]
    public virtual CourseLearningOutcome id_CLONavigation { get; set; } = null!;

    [ForeignKey("id_CO")]
    [InverseProperty("CLO_CO_Mappings")]
    public virtual CourseObjective id_CONavigation { get; set; } = null!;

    [ForeignKey("id_program")]
    [InverseProperty("CLO_CO_Mappings")]
    public virtual TrainingProgram? id_programNavigation { get; set; }
}
