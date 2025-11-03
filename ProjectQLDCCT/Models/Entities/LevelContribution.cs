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

    [StringLength(255)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    [StringLength(50)]
    public string? Weight { get; set; }

    public int? Is_Active { get; set; }

    [InverseProperty("Id_levelNavigation")]
    public virtual ICollection<CLO_PI_Mapping> CLO_PI_Mappings { get; set; } = new List<CLO_PI_Mapping>();

    [InverseProperty("Id_LevelNavigation")]
    public virtual ICollection<CLO_PLO_Mapping> CLO_PLO_Mappings { get; set; } = new List<CLO_PLO_Mapping>();
}
