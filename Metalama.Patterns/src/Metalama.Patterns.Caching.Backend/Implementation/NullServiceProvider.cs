// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Implementation;

internal sealed class NullServiceProvider : IServiceProvider
{
    public static IServiceProvider Instance { get; } = new NullServiceProvider();

    private NullServiceProvider() { }

    public object? GetService( Type serviceType ) => null;
}