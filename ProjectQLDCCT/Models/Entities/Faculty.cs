using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Faculty")]
public partial class Faculty
{
    [Key]
    public int id_faculty { get; set; }

    [StringLength(50)]
    public string? code_faciulty { get; set; }

    [StringLength(200)]
    public string? name_faculty { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_year { get; set; }

    [InverseProperty("id_facultyNavigation")]
    public virtual ICollection<Deparment> Deparments { get; set; } = new List<Deparment>();

    [InverseProperty("id_facultyNavigation")]
    public virtual ICollection<TrainingProgram> TrainingPrograms { get; set; } = new List<TrainingProgram>();

    [ForeignKey("id_year")]
    [InverseProperty("Faculties")]
    public virtual Year? id_yearNavigation { get; set; }
}
