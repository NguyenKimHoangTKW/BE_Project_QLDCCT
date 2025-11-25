using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class User
{
    [Key]
    public int id_users { get; set; }

    [StringLength(200)]
    public string? Username { get; set; }

    [StringLength(200)]
    public string? email { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public string? avatar_url { get; set; }

    public int? id_type_users { get; set; }

    public int status { get; set; }

    [InverseProperty("id_userNavigation")]
    public virtual ICollection<ApproveUserSyllabus> ApproveUserSyllabi { get; set; } = new List<ApproveUserSyllabus>();

    [InverseProperty("id_userNavigation")]
    public virtual ICollection<JWTSession> JWTSessions { get; set; } = new List<JWTSession>();

    [InverseProperty("created_byNavigation")]
    public virtual ICollection<OpenSyllabusWindowsCourse> OpenSyllabusWindowsCourses { get; set; } = new List<OpenSyllabusWindowsCourse>();

    [InverseProperty("create_byNavigation")]
    public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();

    [InverseProperty("id_userNavigation")]
    public virtual ICollection<SyllabusDraft> SyllabusDrafts { get; set; } = new List<SyllabusDraft>();

    [InverseProperty("id_userNavigation")]
    public virtual ICollection<TeacherBySubject> TeacherBySubjects { get; set; } = new List<TeacherBySubject>();

    [InverseProperty("id_usersNavigation")]
    public virtual ICollection<UserByFaculProgram> UserByFaculPrograms { get; set; } = new List<UserByFaculProgram>();

    [ForeignKey("id_type_users")]
    [InverseProperty("Users")]
    public virtual TypeUser? id_type_usersNavigation { get; set; }
}
