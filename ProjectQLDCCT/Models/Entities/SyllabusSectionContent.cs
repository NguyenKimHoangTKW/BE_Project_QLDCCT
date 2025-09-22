using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusSectionContent")]
public partial class SyllabusSectionContent
{
    [Key]
    public int id_content { get; set; }

    public int id_section { get; set; }

    public string? content_text { get; set; }

    [ForeignKey("id_section")]
    [InverseProperty("SyllabusSectionContents")]
    public virtual SyllabusSection id_sectionNavigation { get; set; } = null!;
}
