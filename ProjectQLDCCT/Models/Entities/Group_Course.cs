using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("Group_Course")]
public partial class Group_Course
{
    [Key]
    public int id_gr_course { get; set; }

    [StringLength(50)]
    public string? name_gr_course { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_gr_courseNavigation")]
    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
