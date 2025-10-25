using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("ChildrenRelationshipRatingScale")]
public partial class ChildrenRelationshipRatingScale
{
    [Key]
    public int id { get; set; }

    [StringLength(500)]
    public string? name { get; set; }

    public string? description { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_parentRRS { get; set; }

    [ForeignKey("id_parentRRS")]
    [InverseProperty("ChildrenRelationshipRatingScales")]
    public virtual RelationshipRatingScale? id_parentRRSNavigation { get; set; }
}
