using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("LevelContribution")]
public partial class LevelContribution
{
    [Key]
    public int id { get; set; }

    [StringLength(10)]
    public string? Code { get; set; }

    public string? Description { get; set; }

    public int? id_faculty { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("Id_levelNavigation")]
    public virtual ICollection<CLO_PI_Mapping> CLO_PI_Mappings { get; set; } = new List<CLO_PI_Mapping>();

    [InverseProperty("Id_LevelNavigation")]
    public virtual ICollection<CLO_PLO_Mapping> CLO_PLO_Mappings { get; set; } = new List<CLO_PLO_Mapping>();

    [InverseProperty("id_levelcontributonNavigation")]
    public virtual ICollection<ContributionMatrix> ContributionMatrices { get; set; } = new List<ContributionMatrix>();

    [ForeignKey("id_faculty")]
    [InverseProperty("LevelContributions")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
