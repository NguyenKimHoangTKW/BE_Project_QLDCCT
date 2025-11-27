using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("ProgramLearningOutcome")]
public partial class ProgramLearningOutcome
{
    [Key]
    public int Id_Plo { get; set; }

    public int? Id_Program { get; set; }

    [StringLength(50)]
    public string? code { get; set; }

    public int? id_key_semester { get; set; }

    public string? Description { get; set; }

    public int? order_index { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("Id_PLONavigation")]
    public virtual ICollection<PerformanceIndicator> PerformanceIndicators { get; set; } = new List<PerformanceIndicator>();

    [ForeignKey("id_key_semester")]
    [InverseProperty("ProgramLearningOutcomes")]
    public virtual KeyYearSemester? id_key_semesterNavigation { get; set; }
}
