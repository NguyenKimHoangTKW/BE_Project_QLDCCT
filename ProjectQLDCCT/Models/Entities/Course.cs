using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Course")]
public partial class Course
{
    [Key]
    public int id_course { get; set; }

    public int? id_program { get; set; }

    public int? id_deparment { get; set; }

    [StringLength(50)]
    public string? code_course { get; set; }

    [StringLength(500)]
    public string? name_course { get; set; }

    public int? credits { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();

    [ForeignKey("id_deparment")]
    [InverseProperty("Courses")]
    public virtual Deparment? id_deparmentNavigation { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("Courses")]
    public virtual TrainingProgram? id_programNavigation { get; set; }
}
