using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Deparment")]
public partial class Deparment
{
    [Key]
    public int id_deparment { get; set; }

    public int? id_faculty { get; set; }

    [StringLength(50)]
    public string? code_deparment { get; set; }

    [StringLength(200)]
    public string? name_deparment { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_deparmentNavigation")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    [InverseProperty("id_deparmentNavigation")]
    public virtual ICollection<TrainingProgram> TrainingPrograms { get; set; } = new List<TrainingProgram>();

    [ForeignKey("id_faculty")]
    [InverseProperty("Deparments")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
