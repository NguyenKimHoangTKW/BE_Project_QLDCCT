using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Syllabus")]
public partial class Syllabus
{
    [Key]
    public int id_syllabus { get; set; }

    public int? id_course { get; set; }

    public int? id_civilSer { get; set; }

    [StringLength(50)]
    public string? status { get; set; }

    public int? version { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<CLO> CLOs { get; set; } = new List<CLO>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusApproval> SyllabusApprovals { get; set; } = new List<SyllabusApproval>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusAssessment> SyllabusAssessments { get; set; } = new List<SyllabusAssessment>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusSection> SyllabusSections { get; set; } = new List<SyllabusSection>();

    [ForeignKey("id_civilSer")]
    [InverseProperty("Syllabi")]
    public virtual CivilServant? id_civilSerNavigation { get; set; }

    [ForeignKey("id_course")]
    [InverseProperty("Syllabi")]
    public virtual Course? id_courseNavigation { get; set; }
}
