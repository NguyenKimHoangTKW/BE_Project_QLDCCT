using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SessionPLO")]
public partial class SessionPLO
{
    [Key]
    public int id_plo { get; set; }

    [StringLength(50)]
    public string? name_clo { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_gr_plo { get; set; }

    [InverseProperty("id_ploNavigation")]
    public virtual ICollection<SectionPLIbyPLO> SectionPLIbyPLOs { get; set; } = new List<SectionPLIbyPLO>();

    [ForeignKey("id_gr_plo")]
    [InverseProperty("SessionPLOs")]
    public virtual GroupPLO? id_gr_ploNavigation { get; set; }
}
