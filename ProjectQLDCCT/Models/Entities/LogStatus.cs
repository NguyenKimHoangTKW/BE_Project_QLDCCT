using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("LogStatus")]
public partial class LogStatus
{
    [Key]
    public int id { get; set; }

    [StringLength(250)]
    public string? name { get; set; }

    [InverseProperty("id_statusNavigation")]
    public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();
}
