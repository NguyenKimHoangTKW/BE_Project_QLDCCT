using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class CourseObjective
{
    [Key]
    public int id { get; set; }

    [StringLength(200)]
    public string? name_CO { get; set; }

    public string? describe_CO { get; set; }

    public string? typeOfCapacity { get; set; }

    public int? id_faculty { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_CONavigation")]
    public virtual ICollection<CLO_CO_Mapping> CLO_CO_Mappings { get; set; } = new List<CLO_CO_Mapping>();

    [ForeignKey("id_faculty")]
    [InverseProperty("CourseObjectives")]
    public virtual Faculty? id_facultyNavigation { get; set; }
}
