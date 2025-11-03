using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class CourseLearningOutcome
{
    [Key]
    public int id { get; set; }

    [StringLength(20)]
    public string? name_CLO { get; set; }

    public string? describe_CLO { get; set; }

    public int? id_faculty { get; set; }

    public int? program_id { get; set; }

    public string? bloom_level { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_CLONavigation")]
    public virtual ICollection<CLO_CO_Mapping> CLO_CO_Mappings { get; set; } = new List<CLO_CO_Mapping>();

    [InverseProperty("Id_CLONavigation")]
    public virtual ICollection<CLO_PI_Mapping> CLO_PI_Mappings { get; set; } = new List<CLO_PI_Mapping>();

    [InverseProperty("id_CLONavigation")]
    public virtual ICollection<CLO_PLO_Mapping> CLO_PLO_Mappings { get; set; } = new List<CLO_PLO_Mapping>();

    [ForeignKey("id_faculty")]
    [InverseProperty("CourseLearningOutcomes")]
    public virtual Faculty? id_facultyNavigation { get; set; }

    [ForeignKey("program_id")]
    [InverseProperty("CourseLearningOutcomes")]
    public virtual TrainingProgram? program { get; set; }
}
