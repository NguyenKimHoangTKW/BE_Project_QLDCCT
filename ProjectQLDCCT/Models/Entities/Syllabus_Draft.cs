using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Syllabus_Draft")]
public partial class Syllabus_Draft
{
    [Key]
    public int id { get; set; }

    public int? id_syllabus { get; set; }

    public string? draft_json { get; set; }

    public int? update_time { get; set; }

    [ForeignKey("id_syllabus")]
    [InverseProperty("Syllabus_Drafts")]
    public virtual Syllabus? id_syllabusNavigation { get; set; }
}
