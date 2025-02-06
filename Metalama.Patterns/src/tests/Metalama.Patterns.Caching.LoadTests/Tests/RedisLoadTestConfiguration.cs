// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.LoadTests.Tests;

internal sealed record RedisLoadTestConfiguration(
    int ClientsCount = default,
    Interval ValueKeyLength = default,
    int ValueKeysCount = default,
    Interval ValueKeyExpiry = default,
    Interval ValueLength = default,
    Interval DependencyKeyLength = default,
    int DependencyKeysCount = default,
    Interval DependenciesPerValueCount = default,
    Interval ValuesPerSharedDependency = default,
    int CollectorsCount = default )
    : LoadTestConfiguration(
        ClientsCount,
        ValueKeyLength,
        ValueKeysCount,
        ValueKeyExpiry,
        ValueLength,
        DependencyKeyLength,
        DependencyKeysCount,
        DependenciesPerValueCount,
        ValuesPerSharedDependency );