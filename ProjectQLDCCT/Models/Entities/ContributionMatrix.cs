using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("ContributionMatrix")]
public partial class ContributionMatrix
{
    [Key]
    public int id_CM { get; set; }

    public int? id_course { get; set; }

    public int? Id_PI { get; set; }

    public int? id_levelcontributon { get; set; }

    [ForeignKey("Id_PI")]
    [InverseProperty("ContributionMatrices")]
    public virtual PerformanceIndicator? Id_PINavigation { get; set; }

    [ForeignKey("id_course")]
    [InverseProperty("ContributionMatrices")]
    public virtual Course? id_courseNavigation { get; set; }

    [ForeignKey("id_levelcontributon")]
    [InverseProperty("ContributionMatrices")]
    public virtual LevelContribution? id_levelcontributonNavigation { get; set; }
}
