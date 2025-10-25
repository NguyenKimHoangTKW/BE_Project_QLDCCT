using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

public partial class JWTSession
{
    [Key]
    public int id { get; set; }

    public int? id_user { get; set; }

    public string? token { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? ExpiresAt { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime? CreatedAt { get; set; }

    public string? DeviceName { get; set; }

    public string? IpAddress { get; set; }

    [ForeignKey("id_user")]
    [InverseProperty("JWTSessions")]
    public virtual User? id_userNavigation { get; set; }
}
