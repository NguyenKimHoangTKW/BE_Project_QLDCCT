using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class SyllabusDraft
{
    [Key]
    public int id { get; set; }

    public int? id_syllabus { get; set; }

    public string? section_code { get; set; }

    public string? content_code { get; set; }

    public int? id_user { get; set; }

    public int? update_time { get; set; }

    [ForeignKey("id_syllabus")]
    [InverseProperty("SyllabusDrafts")]
    public virtual Syllabus? id_syllabusNavigation { get; set; }

    [ForeignKey("id_user")]
    [InverseProperty("SyllabusDrafts")]
    public virtual User? id_userNavigation { get; set; }
}
