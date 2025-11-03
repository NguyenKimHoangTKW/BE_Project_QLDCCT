using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusTemplateSection")]
public partial class SyllabusTemplateSection
{
    [Key]
    public int id_template_section { get; set; }

    public int id_template { get; set; }

    [StringLength(50)]
    public string section_code { get; set; } = null!;

    [StringLength(200)]
    public string section_name { get; set; } = null!;

    public int? is_required { get; set; }

    public int? order_index { get; set; }

    public int? id_contentType { get; set; }

    public int? id_dataBinding { get; set; }

    [InverseProperty("id_template_sectionNavigation")]
    public virtual ICollection<SyllabusSection> SyllabusSections { get; set; } = new List<SyllabusSection>();

    [ForeignKey("id_contentType")]
    [InverseProperty("SyllabusTemplateSections")]
    public virtual ContentType? id_contentTypeNavigation { get; set; }

    [ForeignKey("id_dataBinding")]
    [InverseProperty("SyllabusTemplateSections")]
    public virtual DataBinding? id_dataBindingNavigation { get; set; }

    [ForeignKey("id_template")]
    [InverseProperty("SyllabusTemplateSections")]
    public virtual SyllabusTemplate id_templateNavigation { get; set; } = null!;
}
