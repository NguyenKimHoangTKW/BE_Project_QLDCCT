using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class FunctionUser
{
    [Key]
    public int id_func_users { get; set; }

    [StringLength(200)]
    public string? code_func_users { get; set; }

    [StringLength(200)]
    public string? name_func_users { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public int? id_type_users { get; set; }

    [ForeignKey("id_type_users")]
    [InverseProperty("FunctionUsers")]
    public virtual TypeUser? id_type_usersNavigation { get; set; }
}
