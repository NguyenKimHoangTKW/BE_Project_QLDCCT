using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SectionPLIbyPLO")]
public partial class SectionPLIbyPLO
{
    [Key]
    public int id_pli { get; set; }

    [StringLength(50)]
    public string? name_pli { get; set; }

    public int? id_plo { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [ForeignKey("id_plo")]
    [InverseProperty("SectionPLIbyPLOs")]
    public virtual SessionPLO? id_ploNavigation { get; set; }
}
