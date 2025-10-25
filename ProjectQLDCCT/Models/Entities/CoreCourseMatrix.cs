using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("CoreCourseMatrix")]
public partial class CoreCourseMatrix
{
    [Key]
    public int id { get; set; }

    [StringLength(250)]
    public string? name_matrix { get; set; }

    [StringLength(500)]
    public string? description { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_program { get; set; }

    [StringLength(50)]
    public string? version { get; set; }

    [InverseProperty("id_core_course_matrixNavigation")]
    public virtual ICollection<RatingScaleMatrix> RatingScaleMatrices { get; set; } = new List<RatingScaleMatrix>();

    [InverseProperty("id_core_rating_scale_matrixNavigation")]
    public virtual ICollection<RelationshipRatingScale> RelationshipRatingScales { get; set; } = new List<RelationshipRatingScale>();

    [InverseProperty("is_CoreMatrixNavigation")]
    public virtual ICollection<SyllabusTemplateSection> SyllabusTemplateSections { get; set; } = new List<SyllabusTemplateSection>();

    [ForeignKey("id_program")]
    [InverseProperty("CoreCourseMatrices")]
    public virtual TrainingProgram? id_programNavigation { get; set; }
}
