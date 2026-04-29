# MatheusReichert.Aspire.ApachePulsar

Apache Pulsar integration for .NET Aspire. This component allows you to easily orchestrate Apache Pulsar containers along with the Dekaf UI manager.

> [!WARNING]
> **Development Only:** This integration is currently designed and intended for **local development and testing purposes only**. It uses standalone mode for Pulsar and provides quick orchestration for dev inner-loop.

## Features

- **Apache Pulsar**: Standalone broker with binary (6650) and HTTP (8080) endpoints.
- **Dekaf UI**: Integrated web manager for Pulsar topics, producers, and consumers.
- **Persistence**: Support for named volumes to persist data across restarts.
- **Health Checks**: Built-in HTTP health probes to ensure Pulsar is ready before dependent services start.

## Getting Started

### Installation

Add the project reference to your Aspire AppHost project.

### Usage

In your `Program.cs` of the AppHost:

```csharp
using MatheusReichert.Aspire.ApachePulsar;

var builder = DistributedApplication.CreateBuilder(args);

var pulsar = builder.AddPulsar("pulsar")
    .WithDataVolume()
    .WithDekaf();

builder.Build().Run();
```

## Configuration

The Pulsar resource is configured with several development-optimized environment variables:
- Quorum sizes set to 1 for standalone compatibility.
- Memory limits tuned for local execution.
- Auto-recovery and disk usage thresholds adjusted for dev environments.

## License

MIT
