namespace Licensing.Domain.Rules;

public sealed record RuleResult(IReadOnlyList<string> Reasons)
{
    public static RuleResult Pass { get; } = new([]);

    public bool Passed => Reasons.Count == 0;

    public string? Details => Passed ? null : string.Join("; ", Reasons);
}