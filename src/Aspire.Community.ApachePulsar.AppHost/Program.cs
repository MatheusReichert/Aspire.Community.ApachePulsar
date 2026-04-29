using Aspire.Community.ApachePulsar;

var builder = DistributedApplication.CreateBuilder(args);

var pulsar = builder.AddPulsar("pulsar")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDekaf();

builder.Build().Run();
