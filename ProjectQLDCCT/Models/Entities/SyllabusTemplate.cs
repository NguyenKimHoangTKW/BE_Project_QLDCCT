using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusTemplate")]
public partial class SyllabusTemplate
{
    [Key]
    public int id_template { get; set; }

    [StringLength(200)]
    public string template_name { get; set; } = null!;

    public bool? is_default { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_templateNavigation")]
    public virtual ICollection<SyllabusTemplateSection> SyllabusTemplateSections { get; set; } = new List<SyllabusTemplateSection>();
}
