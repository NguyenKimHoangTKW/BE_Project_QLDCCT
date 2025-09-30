using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Student")]
public partial class Student
{
    [Key]
    public int id_student { get; set; }

    [StringLength(50)]
    public string? code_student { get; set; }

    [StringLength(50)]
    public string? name_student { get; set; }

    public int? id_class { get; set; }

    public int? tim_cre { get; set; }

    public int? time_up { get; set; }

    [ForeignKey("id_class")]
    [InverseProperty("Students")]
    public virtual Class? id_classNavigation { get; set; }
}
