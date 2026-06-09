using System.Security.Claims;

namespace _211system.Services;

public static class MedicalPersonnelAuthorization
{
    private static readonly IReadOnlyDictionary<string, int> RankOrder =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Kierownik Szpitala"] = 3,
            ["Lekarz"] = 2,
            ["Medyk"] = 1
        };

    public static int GetActorRank(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin")) return 100;
        if (user.IsInRole("Kierownik Szpitala")) return 3;
        if (user.IsInRole("Lekarz")) return 2;
        if (user.IsInRole("Medyk")) return 1;
        return 0;
    }

    public static int GetStaffRankValue(string? rank) =>
        rank != null && RankOrder.TryGetValue(rank, out var value) ? value : 0;

    public static bool CanManageTarget(
        ClaimsPrincipal user,
        int actorRank,
        Guid? actorHospitalId,
        Guid targetHospitalId,
        string? targetRank)
    {
        if (user.IsInRole("Admin")) return true;
        if (!actorHospitalId.HasValue || actorHospitalId.Value != targetHospitalId) return false;
        return actorRank > GetStaffRankValue(targetRank);
    }

    public static bool CanAssignRank(ClaimsPrincipal user, int actorRank, string? newRank)
    {
        if (user.IsInRole("Admin")) return true;
        return actorRank > GetStaffRankValue(newRank);
    }
}
