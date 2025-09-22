using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("LogOperation")]
public partial class LogOperation
{
    [Key]
    public int id_log { get; set; }

    public string? deception_operation { get; set; }

    public int? time_operation { get; set; }
}
