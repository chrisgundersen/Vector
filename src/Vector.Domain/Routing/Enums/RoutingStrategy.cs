namespace Vector.Domain.Routing.Enums;

/// <summary>
/// Strategy for routing submissions to underwriters.
/// </summary>
public enum RoutingStrategy
{
    /// <summary>
    /// Route to a specific underwriter.
    /// </summary>
    Direct = 0,

    /// <summary>
    /// Route based on producer-underwriter pairing.
    /// </summary>
    ProducerPairing = 1,

    /// <summary>
    /// Route using round-robin among available underwriters.
    /// </summary>
    RoundRobin = 2,

    /// <summary>
    /// Route based on underwriter capacity and workload.
    /// </summary>
    LoadBalanced = 3,

    /// <summary>
    /// Route based on underwriter specialty/expertise.
    /// </summary>
    SpecialtyBased = 4,

    /// <summary>
    /// Route to an underwriter queue for manual assignment.
    /// </summary>
    ManualQueue = 5
}
