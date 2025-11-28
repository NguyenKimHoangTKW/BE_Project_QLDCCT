using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Notification")]
public partial class Notification
{
    [Key]
    public int id_notification { get; set; }

    public int? id_user { get; set; }

    public int? id_program { get; set; }

    [StringLength(255)]
    public string? title { get; set; }

    public string? message { get; set; }

    [StringLength(50)]
    public string? type { get; set; }

    public int? create_time { get; set; }

    public bool? is_read { get; set; }

    [StringLength(255)]
    public string? link { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("Notifications")]
    public virtual TrainingProgram? id_programNavigation { get; set; }

    [ForeignKey("id_user")]
    [InverseProperty("Notifications")]
    public virtual User? id_userNavigation { get; set; }
}
