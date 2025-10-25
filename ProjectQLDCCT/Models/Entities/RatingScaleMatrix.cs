using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("RatingScaleMatrix")]
public partial class RatingScaleMatrix
{
    [Key]
    public int id { get; set; }

    [StringLength(250)]
    public string? name_rating_scale { get; set; }

    [StringLength(10)]
    public string? description { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_core_course_matrix { get; set; }

    [ForeignKey("id_core_course_matrix")]
    [InverseProperty("RatingScaleMatrices")]
    public virtual CoreCourseMatrix? id_core_course_matrixNavigation { get; set; }
}
