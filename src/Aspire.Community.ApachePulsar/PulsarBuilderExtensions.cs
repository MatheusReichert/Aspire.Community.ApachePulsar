// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Community.ApachePulsar;

/// <summary>
/// Provides extension methods for adding Apache Pulsar resources to the application model.
/// </summary>
public static class PulsarBuilderExtensions
{
    private const int PulsarBrokerPort = 6650;
    private const int PulsarHttpPort = 8080;
    private const string DataTarget = "/pulsar/data";

    /// <summary>
    /// Adds an Apache Pulsar resource to the application. A container is used for local development.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="PulsarContainerImageTags.Tag"/> tag of the <inheritdoc cref="PulsarContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="port">The host port of the Pulsar broker.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PulsarContainerResource}"/>.</returns>
    public static IResourceBuilder<PulsarContainerResource> AddPulsar(
        this IDistributedApplicationBuilder builder,
        [ResourceName] string name,
        int? port = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);

        var pulsar = new PulsarContainerResource(name);

        return builder.AddResource(pulsar)
            .WithEndpoint(targetPort: PulsarBrokerPort, port: port, name: PulsarContainerResource.PrimaryEndpointName)
            .WithEndpoint(targetPort: PulsarHttpPort, name: PulsarContainerResource.HttpEndpointName, scheme: "http")
            .WithImage(PulsarContainerImageTags.Image, PulsarContainerImageTags.Tag)
            .WithImageRegistry(PulsarContainerImageTags.Registry)
            .WithEnvironment("PULSAR_MEM", "-Xms512m -Xmx1g")
            .WithEnvironment("PULSAR_PREFIX_journalSyncData", "true")
            .WithEnvironment("PULSAR_PREFIX_autoRecoveryDaemonEnabled", "true")
            .WithEnvironment("PULSAR_PREFIX_readOnlyModeEnabled", "true")
            .WithEnvironment("PULSAR_PREFIX_persistBookieStatusEnabled", "false")
            .WithEnvironment("PULSAR_PREFIX_diskUsageThreshold", "0.95")
            .WithEnvironment("PULSAR_PREFIX_diskUsageWarnThreshold", "0.90")
            .WithEnvironment("PULSAR_PREFIX_journalFlushWhenQueueEmpty", "true")
            .WithEnvironment("PULSAR_PREFIX_managedLedgerDefaultEnsembleSize", "1")
            .WithEnvironment("PULSAR_PREFIX_managedLedgerDefaultWriteQuorum", "1")
            .WithEnvironment("PULSAR_PREFIX_managedLedgerDefaultAckQuorum", "1")
            .WithArgs("bin/pulsar", "standalone", "--no-functions-worker", "--no-stream-storage")
            .WithHttpHealthCheck(endpointName: PulsarContainerResource.HttpEndpointName, path: "/admin/v2/brokers/health");
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarContainerResource> WithDataVolume(
        this IResourceBuilder<PulsarContainerResource> builder,
        string? name = null,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), DataTarget, isReadOnly);
    }

    /// <summary>
    /// Adds a bind mount for the data folder to a Pulsar container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<PulsarContainerResource> WithDataBindMount(
        this IResourceBuilder<PulsarContainerResource> builder,
        string source,
        bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(source);

        return builder.WithBindMount(source, DataTarget, isReadOnly);
    }
    /// <summary>
    /// Adds a Dekaf UI container to the application.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="PulsarContainerImageTags.DekafUiTag"/> tag of the <inheritdoc cref="PulsarContainerImageTags.DekafUiImage"/> container image.
    /// </remarks>
    /// <param name="builder">The Pulsar server resource builder.</param>
    /// <param name="configureContainer">Configuration callback for Dekaf UI container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{PulsarContainerResource}"/>.</returns>
    public static IResourceBuilder<PulsarContainerResource> WithDekaf(
        this IResourceBuilder<PulsarContainerResource> builder,
        Action<IResourceBuilder<DekafUIContainerResource>>? configureContainer = null,
        string? containerName = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<DekafUIContainerResource>().SingleOrDefault() is { } existingDekafUIResource)
        {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingDekafUIResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else
        {
            containerName ??= "dekaf";

            var dekafUi = new DekafUIContainerResource(containerName);
            var dekafUiBuilder = builder.ApplicationBuilder.AddResource(dekafUi)
                .WithImage(PulsarContainerImageTags.DekafUiImage, PulsarContainerImageTags.DekafUiTag)
                .WithImageRegistry(PulsarContainerImageTags.Registry)
                .WithHttpEndpoint(targetPort: 8090, name: "http")
                .WaitFor(builder)
                .ExcludeFromManifest();

            builder.ApplicationBuilder.Eventing.Subscribe<BeforeResourceStartedEvent>(dekafUi, (e, ct) =>
            {
                var pulsarResources = builder.ApplicationBuilder.Resources.OfType<PulsarContainerResource>();

                foreach (var pulsarResource in pulsarResources)
                {
                    var webEndpoint = pulsarResource.GetEndpoint(PulsarContainerResource.HttpEndpointName);
                    var brokerEndpoint = pulsarResource.PrimaryEndpoint;

                    dekafUiBuilder.WithEnvironment(context =>
                    {
                        var webUrl = context.ExecutionContext.IsRunMode
                            ? ReferenceExpression.Create($"http://{webEndpoint.Resource.Name}:{webEndpoint.Property(EndpointProperty.TargetPort)}")
                            : ReferenceExpression.Create($"http://{webEndpoint.Property(EndpointProperty.HostAndPort)}");

                        var brokerUrl = context.ExecutionContext.IsRunMode
                            ? ReferenceExpression.Create($"pulsar://{brokerEndpoint.Resource.Name}:{brokerEndpoint.Property(EndpointProperty.TargetPort)}")
                            : ReferenceExpression.Create($"pulsar://{brokerEndpoint.Property(EndpointProperty.HostAndPort)}");

                        context.EnvironmentVariables["DEKAF_PULSAR_WEB_URL"] = webUrl;
                        context.EnvironmentVariables["DEKAF_PULSAR_BROKER_URL"] = brokerUrl;
                        
                        var dekafEndpointAnn = dekafUi.Annotations.OfType<EndpointAnnotation>().Single(a => a.Name == "http");
                        var dynamicPort = dekafEndpointAnn.AllocatedEndpoint?.Port ?? 8090;
                        context.EnvironmentVariables["DEKAF_PUBLIC_BASE_URL"] = $"http://localhost:{dynamicPort}";
                    });
                }

                return Task.CompletedTask;
            });

            configureContainer?.Invoke(dekafUiBuilder);

            return builder;
        }
    }

    /// <summary>
    /// Configures the host port that the Dekaf UI resource is exposed on instead of using a randomly assigned port.
    /// </summary>
    /// <param name="builder">The resource builder for Dekaf UI.</param>
    /// <param name="port">The port to bind on the host. If <see langword="null"/> is used, a random port will be assigned.</param>
    /// <returns>The resource builder for Dekaf UI.</returns>
    public static IResourceBuilder<DekafUIContainerResource> WithHostPort(this IResourceBuilder<DekafUIContainerResource> builder, int? port)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithEndpoint("http", endpoint =>
        {
            endpoint.Port = port;
        });
    }
}
