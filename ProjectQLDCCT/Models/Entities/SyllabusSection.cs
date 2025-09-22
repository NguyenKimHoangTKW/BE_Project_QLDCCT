using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("SyllabusSection")]
public partial class SyllabusSection
{
    [Key]
    public int id_section { get; set; }

    public int id_syllabus { get; set; }

    public int? id_template_section { get; set; }

    [InverseProperty("id_sectionNavigation")]
    public virtual ICollection<SyllabusSectionContent> SyllabusSectionContents { get; set; } = new List<SyllabusSectionContent>();

    [ForeignKey("id_syllabus")]
    [InverseProperty("SyllabusSections")]
    public virtual Syllabus id_syllabusNavigation { get; set; } = null!;

    [ForeignKey("id_template_section")]
    [InverseProperty("SyllabusSections")]
    public virtual SyllabusTemplateSection? id_template_sectionNavigation { get; set; }
}
