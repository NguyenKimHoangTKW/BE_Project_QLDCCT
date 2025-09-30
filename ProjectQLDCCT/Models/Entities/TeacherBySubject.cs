using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("TeacherBySubject")]
public partial class TeacherBySubject
{
    [Key]
    public int id_teacherbysubject { get; set; }

    public int? id_user { get; set; }

    public int? id_course { get; set; }

    [ForeignKey("id_course")]
    [InverseProperty("TeacherBySubjects")]
    public virtual Course? id_courseNavigation { get; set; }

    [ForeignKey("id_user")]
    [InverseProperty("TeacherBySubjects")]
    public virtual User? id_userNavigation { get; set; }
}
