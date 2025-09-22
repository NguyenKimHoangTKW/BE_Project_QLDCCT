using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class CLOStatistic
{
    [Key]
    public int id_clo_stat { get; set; }

    public int id_clo { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? achieved_rate { get; set; }

    [Column(TypeName = "decimal(5, 2)")]
    public decimal? average_score { get; set; }

    public int? time_stat { get; set; }

    [ForeignKey("id_clo")]
    [InverseProperty("CLOStatistics")]
    public virtual CLO id_cloNavigation { get; set; } = null!;
}
