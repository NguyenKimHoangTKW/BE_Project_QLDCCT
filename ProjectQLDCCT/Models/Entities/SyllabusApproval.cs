using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusApproval")]
public partial class SyllabusApproval
{
    [Key]
    public int id_approval { get; set; }

    public int id_syllabus { get; set; }

    [StringLength(50)]
    public string status { get; set; } = null!;

    public string? comment { get; set; }

    public int? time_action { get; set; }

    [ForeignKey("id_syllabus")]
    [InverseProperty("SyllabusApprovals")]
    public virtual Syllabus id_syllabusNavigation { get; set; } = null!;
}
