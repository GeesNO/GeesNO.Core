namespace GeesNO.Core.Runtime.Observability;

internal sealed class RuntimeInfrastructureFailure : RuntimeObservabilityFailure
{
    internal RuntimeInfrastructureFailure(
        RuntimeInfrastructureComponent component,
        long? relatedSequence,
        Exception exception)
        : base(exception)
    {
        if (!Enum.IsDefined(component))
        {
            throw new ArgumentOutOfRangeException(nameof(component), component, "The component is not defined.");
        }

        if (relatedSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(relatedSequence),
                relatedSequence,
                "The related sequence must be positive when provided.");
        }

        Component = component;
        RelatedSequence = relatedSequence;
    }

    internal RuntimeInfrastructureComponent Component { get; }

    internal long? RelatedSequence { get; }
}
