using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("OpenSyllabusWindowsCourse")]
public partial class OpenSyllabusWindowsCourse
{
    [Key]
    public int id { get; set; }

    public int? id_course { get; set; }

    public int? open_time { get; set; }

    public int? close_time { get; set; }

    public string? reason { get; set; }

    public int? created_by { get; set; }

    public int? is_open { get; set; }

    [ForeignKey("created_by")]
    [InverseProperty("OpenSyllabusWindowsCourses")]
    public virtual User? created_byNavigation { get; set; }

    [ForeignKey("id_course")]
    [InverseProperty("OpenSyllabusWindowsCourses")]
    public virtual Course? id_courseNavigation { get; set; }
}
