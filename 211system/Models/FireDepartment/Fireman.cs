using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    public Guid PDepartmentId { get; set; }
    public virtual FDepartment Department { get; set; }

    public string FireAccountId { get; set; }
    [ForeignKey(nameof(FireAccountId))]
    public virtual IdentityUser FireAccount { get; set; }

    public Fireman()
	{
	}
}
