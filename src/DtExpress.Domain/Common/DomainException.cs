namespace DtExpress.Domain.Common;

/// <summary>
/// Base exception for all domain rule violations.
/// Every domain exception carries a machine-readable error code.
/// </summary>
public class DomainException : Exception
{
    /// <summary>Machine-readable error code, e.g. "INVALID_TRANSITION".</summary>
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}

/// <summary>
/// Thrown when an order state transition is not allowed from the current state.
/// State Pattern: invalid action for current state.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    public string CurrentState { get; }
    public string AttemptedAction { get; }

    public InvalidStateTransitionException(string currentState, string attemptedAction)
        : base(
            "INVALID_TRANSITION",
            $"Cannot perform '{attemptedAction}' on order in '{currentState}' state.")
    {
        CurrentState = currentState;
        AttemptedAction = attemptedAction;
    }
}

/// <summary>
/// Thrown when a carrier code cannot be resolved to an adapter.
/// Factory Pattern: unknown key in registry.
/// </summary>
public class CarrierNotFoundException : DomainException
{
    public string CarrierCode { get; }

    public CarrierNotFoundException(string carrierCode)
        : base(
            "CARRIER_NOT_FOUND",
            $"No carrier adapter registered for code '{carrierCode}'.")
    {
        CarrierCode = carrierCode;
    }
}

/// <summary>
/// Thrown when a routing strategy name cannot be resolved.
/// Factory Pattern: unknown key in registry.
/// </summary>
public class StrategyNotFoundException : DomainException
{
    public string StrategyName { get; }

    public StrategyNotFoundException(string strategyName)
        : base(
            "STRATEGY_NOT_FOUND",
            $"No route strategy registered with name '{strategyName}'.")
    {
        StrategyName = strategyName;
    }
}
