using Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Reviews;


public class Review : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public int Rating { get; set; }             // 1–5
    public string Comment { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true; // flip default if you want moderation
}
