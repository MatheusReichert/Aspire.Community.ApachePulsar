// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace MatheusReichert.Aspire.ApachePulsar;

/// <summary>
/// A resource that represents an Apache Pulsar container.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class PulsarContainerResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    // This endpoint is used for the Pulsar binary protocol (producers/consumers).
    internal const string PrimaryEndpointName = "tcp";

    // This endpoint is used for the Pulsar HTTP/admin REST API.
    internal const string HttpEndpointName = "http";

    private EndpointReference? _primaryEndpoint;

    /// <summary>
    /// Gets the primary endpoint for the Pulsar broker (binary protocol on port 6650).
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the host endpoint reference for the primary endpoint.
    /// </summary>
    public EndpointReferenceExpression Host => PrimaryEndpoint.Property(EndpointProperty.Host);

    /// <summary>
    /// Gets the port endpoint reference for the primary endpoint.
    /// </summary>
    public EndpointReferenceExpression Port => PrimaryEndpoint.Property(EndpointProperty.Port);

    /// <summary>
    /// Gets the connection string expression for the Pulsar broker.
    /// Format: <c>pulsar://{host}:{port}</c>
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"pulsar://{PrimaryEndpoint.Property(EndpointProperty.HostAndPort)}");

    IEnumerable<KeyValuePair<string, ReferenceExpression>> IResourceWithConnectionString.GetConnectionProperties()
    {
        yield return new("Host", ReferenceExpression.Create($"{Host}"));
        yield return new("Port", ReferenceExpression.Create($"{Port}"));
    }
}
