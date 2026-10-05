using GeesNO.Core.Contracts.Capabilities;

namespace GeesNO.Core.Runtime;

/// <summary>
/// Provides an immutable public view of a provider capability registered in a Runtime.
/// </summary>
public sealed class RuntimeCapability
{
    internal RuntimeCapability(
        ProviderId providerId,
        CapabilityDescriptor descriptor,
        CapabilitySupport support)
    {
        ArgumentNullException.ThrowIfNull(providerId);
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(support);

        if (descriptor.Reference != support.Capability)
        {
            throw new ArgumentException("The capability descriptor and support must describe the same capability.");
        }

        ProviderId = providerId;
        Descriptor = descriptor;
        Support = support;
    }

    /// <summary>
    /// Gets the provider that declares the capability.
    /// </summary>
    public ProviderId ProviderId { get; }

    /// <summary>
    /// Gets the public capability descriptor.
    /// </summary>
    public CapabilityDescriptor Descriptor { get; }

    /// <summary>
    /// Gets the public provider support information for the capability.
    /// </summary>
    public CapabilitySupport Support { get; }
}