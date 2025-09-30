using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("GroupPLO")]
public partial class GroupPLO
{
    [Key]
    public int id_gr_plo { get; set; }

    [StringLength(200)]
    public string? name_gr_plo { get; set; }

    [StringLength(500)]
    public string? content { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_gr_ploNavigation")]
    public virtual ICollection<SessionPLO> SessionPLOs { get; set; } = new List<SessionPLO>();
}
