using System;
using System.Collections.Generic;

namespace _211system.DTOs
{
    public class CrewMemberDto
    {
        public Guid MemberId { get; set; }
        public string MemberName { get; set; }
    }

    public class SetCrewDto
    {
        public List<CrewMemberDto> Crew { get; set; } = new();
    }
}
