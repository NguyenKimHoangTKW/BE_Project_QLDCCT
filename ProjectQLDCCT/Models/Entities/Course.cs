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

    public int? id_year { get; set; }

    [StringLength(50)]
    public string? code_course { get; set; }

    [StringLength(500)]
    public string? name_course { get; set; }

    public int? id_gr_course { get; set; }

    public int? id_semester { get; set; }

    public int? credits { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_key_year_semester { get; set; }

    public int? is_obligatory { get; set; }

    public int? is_selfselect { get; set; }

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<TeacherBySubject> TeacherBySubjects { get; set; } = new List<TeacherBySubject>();

    [ForeignKey("id_gr_course")]
    [InverseProperty("Courses")]
    public virtual Group_Course? id_gr_courseNavigation { get; set; }

    [ForeignKey("id_key_year_semester")]
    [InverseProperty("Courses")]
    public virtual KeyYearSemester? id_key_year_semesterNavigation { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("Courses")]
    public virtual TrainingProgram? id_programNavigation { get; set; }

    [ForeignKey("id_semester")]
    [InverseProperty("Courses")]
    public virtual Semester? id_semesterNavigation { get; set; }

    [ForeignKey("id_year")]
    [InverseProperty("Courses")]
    public virtual Year? id_yearNavigation { get; set; }
}
