using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class TypeUser
{
    [Key]
    public int id_type_users { get; set; }

    [StringLength(50)]
    public string? name_type_users { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    [InverseProperty("id_type_usersNavigation")]
    public virtual ICollection<FunctionUser> FunctionUsers { get; set; } = new List<FunctionUser>();

    [InverseProperty("id_type_usersNavigation")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
