using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("MappingCLObyPI")]
public partial class MappingCLObyPI
{
    [Key]
    public int id { get; set; }

    public int? id_CLoMapping { get; set; }

    public int? Id_Level { get; set; }

    public int? Id_PI { get; set; }

    [ForeignKey("Id_Level")]
    [InverseProperty("MappingCLObyPIs")]
    public virtual LevelContribution? Id_LevelNavigation { get; set; }

    [ForeignKey("Id_PI")]
    [InverseProperty("MappingCLObyPIs")]
    public virtual PerformanceIndicator? Id_PINavigation { get; set; }

    [ForeignKey("id_CLoMapping")]
    [InverseProperty("MappingCLObyPIs")]
    public virtual MappingCLOBySyllabus? id_CLoMappingNavigation { get; set; }
}
