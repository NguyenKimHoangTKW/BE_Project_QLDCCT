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

    public int? id_faculty { get; set; }

    [StringLength(50)]
    public string? version { get; set; }

    [InverseProperty("id_CoreCourseMatrixNavigation")]
    public virtual ICollection<CLO_PLO_Mapping> CLO_PLO_Mappings { get; set; } = new List<CLO_PLO_Mapping>();

    [ForeignKey("id_faculty")]
    [InverseProperty("CoreCourseMatrices")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
