using _211system.DTOs.Ai;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace _211system.Services
{
    public interface IAiService
    {
        Task<List<AiDispatchSuggestion>> GetAutoDispatchPlanAsync(AiDispatchRequestDto requestData);
    }
}