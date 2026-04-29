# MatheusReichert.Aspire.ApachePulsar library

Provides extension methods and resource definitions for an Aspire AppHost to configure an Apache Pulsar resource.

## Getting started

### Install the package

In your AppHost project, install the Aspire Apache Pulsar Hosting library with [NuGet](https://www.nuget.org):

```dotnetcli
dotnet add package MatheusReichert.Aspire.ApachePulsar
```

## Usage example

Then, in the _Program.cs_ file of `AppHost`, add a Pulsar resource and consume the connection using the following methods:

```csharp
var pulsar = builder.AddPulsar("messaging")
    .WithDekaf();

var myService = builder.AddProject<Projects.MyService>()
                       .WithReference(pulsar);
```

## Connection Properties

When you reference a Pulsar resource using `WithReference`, the following connection properties are made available to the consuming project:

### Pulsar server

The Pulsar server resource exposes the following connection properties:

| Property Name | Description |
|---------------|-------------|
| `Host` | The host-facing Pulsar broker hostname or IP address |
| `Port` | The host-facing Pulsar broker port |

Aspire exposes each property as an environment variable named `[RESOURCE]_[PROPERTY]`. For instance, the `Host` property of a resource called `messaging` becomes `MESSAGING_HOST`.

## Additional documentation

* https://pulsar.apache.org/docs/4.x/
