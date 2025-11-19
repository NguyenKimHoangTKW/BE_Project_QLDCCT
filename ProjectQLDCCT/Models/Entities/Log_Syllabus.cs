using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Log_Syllabus")]
public partial class Log_Syllabus
{
    [Key]
    public int id_log { get; set; }

    public int? id_syllabus { get; set; }

    public string? content_value { get; set; }

    public int? log_time { get; set; }

    [ForeignKey("id_syllabus")]
    [InverseProperty("Log_Syllabi")]
    public virtual Syllabus? id_syllabusNavigation { get; set; }
}
