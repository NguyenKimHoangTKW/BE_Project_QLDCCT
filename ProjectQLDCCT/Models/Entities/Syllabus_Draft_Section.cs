using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Syllabus_Draft_Section")]
public partial class Syllabus_Draft_Section
{
    [Key]
    public int id { get; set; }

    public int? id_syllabus { get; set; }

    public string? section_json { get; set; }

    public int? update_time { get; set; }
}
