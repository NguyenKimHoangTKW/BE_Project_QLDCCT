using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CLO_PI_Mapping")]
public partial class CLO_PI_Mapping
{
    [Key]
    public int id { get; set; }

    public int? Id_CLO { get; set; }

    public int? id_PI { get; set; }

    public int? Id_level { get; set; }

    public string? Comment { get; set; }

    [ForeignKey("Id_CLO")]
    [InverseProperty("CLO_PI_Mappings")]
    public virtual CourseLearningOutcome? Id_CLONavigation { get; set; }

    [ForeignKey("Id_level")]
    [InverseProperty("CLO_PI_Mappings")]
    public virtual LevelContribution? Id_levelNavigation { get; set; }

    [ForeignKey("id_PI")]
    [InverseProperty("CLO_PI_Mappings")]
    public virtual PerformanceIndicator? id_PINavigation { get; set; }
}
