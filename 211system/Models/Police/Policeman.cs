using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

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

    public string IdentityUserId { get; set; }
    public virtual IdentityUser IdentityUser { get; set; }

    public Policeman()
	{
	}
}
