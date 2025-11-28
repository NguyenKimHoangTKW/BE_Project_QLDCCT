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

    public int? id_teacherbysubject { get; set; }

    public int? id_status { get; set; }

    [StringLength(50)]
    public string? version { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? create_by { get; set; }

    public string? syllabus_json { get; set; }

    public string? returned_content { get; set; }

    public string? edit_content { get; set; }

    public int? is_open_edit_final { get; set; }

    public string? html_export_word { get; set; }

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<ApproveUserSyllabus> ApproveUserSyllabi { get; set; } = new List<ApproveUserSyllabus>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<Log_Syllabus> Log_Syllabi { get; set; } = new List<Log_Syllabus>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<MappingCLOBySyllabus> MappingCLOBySyllabi { get; set; } = new List<MappingCLOBySyllabus>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusApproval> SyllabusApprovals { get; set; } = new List<SyllabusApproval>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusDraft> SyllabusDrafts { get; set; } = new List<SyllabusDraft>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<SyllabusSection> SyllabusSections { get; set; } = new List<SyllabusSection>();

    [InverseProperty("id_syllabusNavigation")]
    public virtual ICollection<Syllabus_Draft> Syllabus_Drafts { get; set; } = new List<Syllabus_Draft>();

    [ForeignKey("create_by")]
    [InverseProperty("Syllabi")]
    public virtual User? create_byNavigation { get; set; }

    [ForeignKey("id_status")]
    [InverseProperty("Syllabi")]
    public virtual LogStatus? id_statusNavigation { get; set; }

    [ForeignKey("id_teacherbysubject")]
    [InverseProperty("Syllabi")]
    public virtual TeacherBySubject? id_teacherbysubjectNavigation { get; set; }
}
