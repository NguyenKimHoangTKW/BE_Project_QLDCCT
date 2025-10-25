using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CourseByKey")]
public partial class CourseByKey
{
    [Key]
    public int id { get; set; }

    public int? id_course { get; set; }

    public int? id_semester { get; set; }

    public int? id_key_semester { get; set; }

    public int? id_program { get; set; }

    [ForeignKey("id_course")]
    [InverseProperty("CourseByKeys")]
    public virtual Course? id_courseNavigation { get; set; }

    [ForeignKey("id_key_semester")]
    [InverseProperty("CourseByKeys")]
    public virtual KeyYearSemester? id_key_semesterNavigation { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("CourseByKeys")]
    public virtual TrainingProgram? id_programNavigation { get; set; }

    [ForeignKey("id_semester")]
    [InverseProperty("CourseByKeys")]
    public virtual Semester? id_semesterNavigation { get; set; }
}
