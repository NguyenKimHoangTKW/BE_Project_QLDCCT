using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("IsCourse")]
public partial class IsCourse
{
    [Key]
    public int id { get; set; }

    [StringLength(50)]
    public string? name { get; set; }

    [InverseProperty("id_isCourseNavigation")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
