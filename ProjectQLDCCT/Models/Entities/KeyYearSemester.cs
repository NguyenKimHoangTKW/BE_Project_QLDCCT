using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("KeyYearSemester")]
public partial class KeyYearSemester
{
    [Key]
    public int id_key_year_semester { get; set; }

    [StringLength(50)]
    public string? code_key_year_semester { get; set; }

    [StringLength(50)]
    public string? name_key_year_semester { get; set; }

    public int? id_faculty { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_key_semesterNavigation")]
    public virtual ICollection<CourseByKey> CourseByKeys { get; set; } = new List<CourseByKey>();

    [ForeignKey("id_faculty")]
    [InverseProperty("KeyYearSemesters")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
