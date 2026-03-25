using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using _211system.Models;
using Microsoft.AspNetCore.Identity;

namespace FireDepartment;
public class Fireman
{
	[Key]
    public Guid Id { get; set; }
	public string Name { get; set; }
	public string Surname { get; set; }
	public string BadgeNumber { get; set; }
	public string Rank { get; set; }

    public Guid FDepartmentId { get; set; }
    public virtual FDepartment Department { get; set; }

    public string FireAccountId { get; set; }
    [ForeignKey(nameof(FireAccountId))]
    public virtual ApplicationUser FireAccount { get; set; }

    public Fireman()
	{
	}
}
