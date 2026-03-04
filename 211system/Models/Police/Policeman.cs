using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Police;
public class Policeman
{
	[Key]
	public Guid Id { get; set; }
	public string Name { get; set; }
	public string Surname { get; set; }
	public string BadgeNumber { get; set; }
	public string Rank { get; set; }

    public Guid PDepartmentId { get; set; }
    public virtual PDepartment Department { get; set; }

    public string PoliceAccountId { get; set; }
    [ForeignKey(nameof(PoliceAccountId))]
    public virtual IdentityUser PoliceAccount { get; set; }

    public Policeman()
	{
	}
}
