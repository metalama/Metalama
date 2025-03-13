// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Utilities;

internal sealed class NullServiceProvider : IServiceProvider
{
    public static IServiceProvider Instance { get; } = new NullServiceProvider();

    private NullServiceProvider() { }

    public object? GetService( Type serviceType ) => null;
}