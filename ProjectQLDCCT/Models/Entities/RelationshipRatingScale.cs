using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("RelationshipRatingScale")]
public partial class RelationshipRatingScale
{
    [Key]
    public int id { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_core_rating_scale_matrix { get; set; }

    [InverseProperty("id_parentRRSNavigation")]
    public virtual ICollection<ChildrenRelationshipRatingScale> ChildrenRelationshipRatingScales { get; set; } = new List<ChildrenRelationshipRatingScale>();

    [ForeignKey("id_core_rating_scale_matrix")]
    [InverseProperty("RelationshipRatingScales")]
    public virtual CoreCourseMatrix? id_core_rating_scale_matrixNavigation { get; set; }
}
