namespace DayTradeBot.domains.appOrchestration;

public class ValidationResults
{
    public bool IsValid { get; set; } = false;
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}