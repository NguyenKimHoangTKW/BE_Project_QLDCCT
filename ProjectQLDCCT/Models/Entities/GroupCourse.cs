using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("GroupCourse")]
[Index("name", Name = "UQ__GroupCou__72E12F1B8C0BD009", IsUnique = true)]
public partial class GroupCourse
{
    [Key]
    public int id { get; set; }

    [StringLength(100)]
    public string name { get; set; } = null!;
}
