using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("UserByFaculProgram")]
public partial class UserByFaculProgram
{
    [Key]
    public int id { get; set; }

    public int? id_faculty { get; set; }

    public int? id_users { get; set; }

    public int? id_program { get; set; }

    [ForeignKey("id_faculty")]
    [InverseProperty("UserByFaculPrograms")]
    public virtual Faculty? id_facultyNavigation { get; set; }

    [ForeignKey("id_program")]
    [InverseProperty("UserByFaculPrograms")]
    public virtual TrainingProgram? id_programNavigation { get; set; }

    [ForeignKey("id_users")]
    [InverseProperty("UserByFaculPrograms")]
    public virtual User? id_usersNavigation { get; set; }
}
