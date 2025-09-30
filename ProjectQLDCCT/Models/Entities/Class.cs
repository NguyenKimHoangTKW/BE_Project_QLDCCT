using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Class")]
public partial class Class
{
    [Key]
    public int id_class { get; set; }

    [StringLength(50)]
    public string? name_class { get; set; }

    public int? id_program { get; set; }

    public int? tim_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_classNavigation")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    [ForeignKey("id_program")]
    [InverseProperty("Classes")]
    public virtual TrainingProgram? id_programNavigation { get; set; }
}
