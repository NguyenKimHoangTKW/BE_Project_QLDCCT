using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Course Objectives")]
public partial class Course_Objective
{
    [Key]
    public int id { get; set; }

    [StringLength(200)]
    public string? name_CO { get; set; }

    public string? describe_CO { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("is_CONavigation")]
    public virtual ICollection<SyllabusTemplateSection> SyllabusTemplateSections { get; set; } = new List<SyllabusTemplateSection>();
}
