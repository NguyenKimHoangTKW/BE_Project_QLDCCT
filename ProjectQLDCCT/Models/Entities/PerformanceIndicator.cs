using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("PerformanceIndicator")]
public partial class PerformanceIndicator
{
    [Key]
    public int Id_PI { get; set; }

    public int? Id_PLO { get; set; }

    [StringLength(50)]
    public string? code { get; set; }

    public string? Description { get; set; }

    public int? order_index { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_PINavigation")]
    public virtual ICollection<CLO_PI_Mapping> CLO_PI_Mappings { get; set; } = new List<CLO_PI_Mapping>();

    [ForeignKey("Id_PLO")]
    [InverseProperty("PerformanceIndicators")]
    public virtual ProgramLearningOutcome? Id_PLONavigation { get; set; }
}
