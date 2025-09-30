﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("TrainingProgram")]
public partial class TrainingProgram
{
    [Key]
    public int id_program { get; set; }

    public int? id_faculty { get; set; }

    [StringLength(50)]
    public string? code_program { get; set; }

    [StringLength(200)]
    public string? name_program { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_programNavigation")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();

    [InverseProperty("id_programNavigation")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();

    [ForeignKey("id_faculty")]
    [InverseProperty("TrainingPrograms")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
