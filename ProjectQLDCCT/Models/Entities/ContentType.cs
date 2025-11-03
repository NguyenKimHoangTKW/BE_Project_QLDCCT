using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("ContentType")]
public partial class ContentType
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string? code { get; set; }

    [StringLength(200)]
    public string? name { get; set; }

    [InverseProperty("id_contentTypeNavigation")]
    public virtual ICollection<SyllabusTemplateSection> SyllabusTemplateSections { get; set; } = new List<SyllabusTemplateSection>();
}
