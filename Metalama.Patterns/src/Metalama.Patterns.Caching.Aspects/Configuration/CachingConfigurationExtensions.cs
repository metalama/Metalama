// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Options;

namespace Metalama.Patterns.Caching.Aspects.Configuration;

[PublicAPI]
[CompileTime]
public static class CachingConfigurationExtensions
{
    public static void ConfigureCaching( this IQuery<IMethod> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    public static void ConfigureCaching( this IQuery<ICompilation> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    public static void ConfigureCaching( this IQuery<INamespace> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }

    public static void ConfigureCaching( this IQuery<INamedType> method, Action<CachingOptionsBuilder> build )
    {
        var builder = new CachingOptionsBuilder();
        build( builder );
        method.SetOptions( builder.Build() );
    }
}