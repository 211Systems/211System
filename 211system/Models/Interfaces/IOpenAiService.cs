namespace _211system.Models.Interfaces;

public interface IOpenAiService
{
    Task<string> GetAdviceAsync(string incidentDescription);
}