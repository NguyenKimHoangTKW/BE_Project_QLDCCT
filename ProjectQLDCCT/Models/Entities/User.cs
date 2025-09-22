using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class User
{
    [Key]
    public int id_users { get; set; }

    [StringLength(200)]
    public string? Username { get; set; }

    [StringLength(200)]
    public string? email { get; set; }

    public int? time_cre { get; set; }

    public int? time_up { get; set; }

    public string? avatar_url { get; set; }

    public int? id_type_users { get; set; }

    public int status { get; set; }

    [ForeignKey("id_type_users")]
    [InverseProperty("Users")]
    public virtual TypeUser? id_type_usersNavigation { get; set; }
}
