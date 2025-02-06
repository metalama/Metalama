// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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