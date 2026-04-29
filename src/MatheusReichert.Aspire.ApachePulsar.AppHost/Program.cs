using MatheusReichert.Aspire.ApachePulsar;

var builder = DistributedApplication.CreateBuilder(args);

var pulsar = builder.AddPulsar("pulsar")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDekaf();

builder.Build().Run();
