using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Semester")]
public partial class Semester
{
    [Key]
    public int id_semester { get; set; }

    [StringLength(50)]
    public string? code_semester { get; set; }

    [StringLength(50)]
    public string? name_semester { get; set; }

    public int? id_faculty { get; set; }

    public int? tim_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_semesterNavigation")]
    public virtual ICollection<CourseByKey> CourseByKeys { get; set; } = new List<CourseByKey>();

    [ForeignKey("id_faculty")]
    [InverseProperty("Semesters")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
