![img|300x200](https://raw.githubusercontent.com/managedcode/Orleans.Balancer/main/logo.png)

# Orleans.Balancer

[![.NET](https://github.com/managedcode/Orleans.Balancer/actions/workflows/dotnet.yml/badge.svg)](https://github.com/managedcode/Orleans.Balancer/actions/workflows/dotnet.yml)
[![Coverage Status](https://coveralls.io/repos/github/managedcode/Orleans.Balancer/badge.svg?branch=main)](https://coveralls.io/github/managedcode/Orleans.Balancer?branch=main)
[![nuget](https://github.com/managedcode/Orleans.Balancer/actions/workflows/nuget.yml/badge.svg?branch=main)](https://github.com/managedcode/Orleans.Balancer/actions/workflows/nuget.yml)
[![CodeQL](https://github.com/managedcode/Orleans.Balancer/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)](https://github.com/managedcode/Orleans.Balancer/actions/workflows/codeql-analysis.yml)

Orleans.Balancer is a library for automatically balancing the number of active Grains in an Orleans distributed system. It allows you to set limits on the number of active Grains, and will automatically deactivate Grains if those limits are reached. It can also perform rebalancing of Grain activations between silos to ensure evenly distributed.

## Motivation
Orleans is a distributed systems platform that makes it easy to build highly scalable, low-latency applications. However, managing the number of active Grains in an Orleans system can be challenging, especially in environments with unpredictable workloads. Orleans.Balancer solves this problem by providing automatic control over the number of active Grains, as well as tools for rebalancing activations between silos. This ensures that your Orleans applications have the resources they need to handle increased workloads without manual intervention. Use it together with https://github.com/managedcode/Keda

## Getting Started
To use Orleans.Balancer, you will need to have an Orleans distributed system set up. Once you have that, you can install Orleans.Balancer using the provided NuGet package.

## Usage
Orleans.Balancer is used by creating an instance of the Orleans.Balancer.Balancer class and passing in the desired settings. The Balancer class provides methods for controlling the number of active Grains, as well as for performing rebalancing operations.

Install package ``` ManagedCode.Orleans.Balancer```

```cs
siloBuilder.UseOrleansBalancer(o => { });
```
## Contributing
We welcome contributions to Orleans.Balancer! If you have an idea for a new feature or have found a bug, please open an issue on GitHub.

## Base on
https://github.com/oising/OrleansContrib.ActivationShedding
