using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Year")]
public partial class Year
{
    [Key]
    public int id_year { get; set; }

    [StringLength(50)]
    public string? name_year { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_yearNavigation")]
    public virtual ICollection<CivilServant> CivilServants { get; set; } = new List<CivilServant>();

    [InverseProperty("id_yearNavigation")]
    public virtual ICollection<Faculty> Faculties { get; set; } = new List<Faculty>();
}
