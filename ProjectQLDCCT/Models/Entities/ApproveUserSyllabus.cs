using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectQLDCCT.Models;

[Table("ApproveUserSyllabus")]
public partial class ApproveUserSyllabus
{
    [Key]
    public int id_ApproveUserSyllabus { get; set; }

    public int? id_syllabus { get; set; }

    public int? id_user { get; set; }

    public bool? is_approve { get; set; }

    public bool? is_key_user { get; set; }

    public bool? is_refuse { get; set; }

    public int? time_request { get; set; }

    public int? time_accept_request { get; set; }

    [ForeignKey("id_syllabus")]
    [InverseProperty("ApproveUserSyllabi")]
    public virtual Syllabus? id_syllabusNavigation { get; set; }

    [ForeignKey("id_user")]
    [InverseProperty("ApproveUserSyllabi")]
    public virtual User? id_userNavigation { get; set; }
}
