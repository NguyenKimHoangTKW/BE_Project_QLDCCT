using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class CivilServant
{
    [Key]
    public int id_civilSer { get; set; }

    [StringLength(50)]
    public string? code_civilSer { get; set; }

    [StringLength(200)]
    public string? fullname_civilSer { get; set; }

    [StringLength(200)]
    public string? email { get; set; }

    public DateOnly? birthday { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_civilSerNavigation")]
    public virtual ICollection<Syllabus> Syllabi { get; set; } = new List<Syllabus>();
}
