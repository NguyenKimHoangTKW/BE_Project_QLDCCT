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

    [StringLength(50)]
    public string? code_course { get; set; }

    [StringLength(500)]
    public string? name_course { get; set; }

    public int? id_gr_course { get; set; }

    public int? credits { get; set; }

    public int? totalTheory { get; set; }

    public int? totalPractice { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_isCourse { get; set; }

    public int? id_key_year_semester { get; set; }

    public int? id_semester { get; set; }

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<ContributionMatrix> ContributionMatrices { get; set; } = new List<ContributionMatrix>();

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<CourseByKey> CourseByKeys { get; set; } = new List<CourseByKey>();

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<OpenSyllabusWindowsCourse> OpenSyllabusWindowsCourses { get; set; } = new List<OpenSyllabusWindowsCourse>();

    [InverseProperty("id_courseNavigation")]
    public virtual ICollection<TeacherBySubject> TeacherBySubjects { get; set; } = new List<TeacherBySubject>();

    [ForeignKey("id_gr_course")]
    [InverseProperty("Courses")]
    public virtual Group_Course? id_gr_courseNavigation { get; set; }

    [ForeignKey("id_isCourse")]
    [InverseProperty("Courses")]
    public virtual IsCourse? id_isCourseNavigation { get; set; }

    [ForeignKey("id_key_year_semester")]
    [InverseProperty("Courses")]
    public virtual KeyYearSemester? id_key_year_semesterNavigation { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("Courses")]
    public virtual TrainingProgram? id_programNavigation { get; set; }

    [ForeignKey("id_semester")]
    [InverseProperty("Courses")]
    public virtual Semester? id_semesterNavigation { get; set; }
}
