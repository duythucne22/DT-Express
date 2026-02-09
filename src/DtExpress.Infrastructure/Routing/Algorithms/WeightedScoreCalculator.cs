using DtExpress.Domain.Routing.Models;

namespace DtExpress.Infrastructure.Routing.Algorithms;

/// <summary>
/// Internal static helper for the Balanced route strategy (Phase 4, Task 4.1).
/// Computes a normalized weighted score between time-optimized and cost-optimized paths
/// so that <c>BalancedRouteStrategy</c> can pick the best trade-off.
/// </summary>
internal static class WeightedScoreCalculator
{
    /// <summary>Default weight for time factor in balanced scoring.</summary>
    internal const decimal DefaultTimeWeight = 0.6m;

    /// <summary>Default weight for cost factor in balanced scoring.</summary>
    internal const decimal DefaultCostWeight = 0.4m;

    /// <summary>
    /// Calculate a weighted score for a given <paramref name="candidate"/> path
    /// relative to a <paramref name="baseline"/> path. Lower score = better balance.
    /// <para>
    /// Time and cost are each normalized to a 0–1 scale (candidate ÷ baseline),
    /// then combined with the given weights. A perfect baseline match yields 1.0.
    /// </para>
    /// </summary>
    /// <param name="candidate">The path being scored.</param>
    /// <param name="baseline">Reference path used for normalization (typically the worst-case).</param>
    /// <param name="timeWeight">Weight for the time component (default 0.6).</param>
    /// <param name="costWeight">Weight for the cost component (default 0.4).</param>
    /// <returns>A decimal score where lower is better.</returns>
    internal static decimal CalculateScore(
        PathResult candidate,
        PathResult baseline,
        decimal timeWeight = DefaultTimeWeight,
        decimal costWeight = DefaultCostWeight)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(baseline);

        // Guard against division by zero if baseline has zero values
        var baselineHours = baseline.TotalDuration.TotalHours;
        var baselineCost = baseline.TotalCost.Amount;

        if (baselineHours <= 0 && baselineCost <= 0)
            return 0m;

        var normalizedTime = baselineHours > 0
            ? (decimal)(candidate.TotalDuration.TotalHours / baselineHours)
            : 0m;

        var normalizedCost = baselineCost > 0
            ? candidate.TotalCost.Amount / baselineCost
            : 0m;

        return normalizedTime * timeWeight + normalizedCost * costWeight;
    }

    /// <summary>
    /// Select the best-balanced path from a set of candidates.
    /// Scores each candidate against the <paramref name="baseline"/> and returns the lowest-scored.
    /// </summary>
    /// <param name="candidates">Paths to evaluate.</param>
    /// <param name="baseline">Reference path for normalization.</param>
    /// <param name="timeWeight">Weight for time (default 0.6).</param>
    /// <param name="costWeight">Weight for cost (default 0.4).</param>
    /// <returns>The candidate with the lowest weighted score, or <c>null</c> if empty.</returns>
    internal static PathResult? SelectBest(
        IReadOnlyList<PathResult> candidates,
        PathResult baseline,
        decimal timeWeight = DefaultTimeWeight,
        decimal costWeight = DefaultCostWeight)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(baseline);

        if (candidates.Count == 0)
            return null;

        PathResult? best = null;
        var bestScore = decimal.MaxValue;

        foreach (var candidate in candidates)
        {
            var score = CalculateScore(candidate, baseline, timeWeight, costWeight);
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }
}
