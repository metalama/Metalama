// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

public sealed class LoggerDependencyInjectionFramework : DefaultDependencyInjectionFramework
{
    public override bool CanHandleDependency( DependencyProperties properties, in ScopedDiagnosticSink diagnostics )
        => properties.DependencyType is INamedType { Name: "ILogger", FullName: "Microsoft.Extensions.Logging.ILogger" };

    protected override DefaultDependencyInjectionStrategy GetStrategy( DependencyProperties properties ) => new InjectionStrategy( properties );

    // Our customized injection strategy. Decides how to create the field or property.
    // We actually have no customization except that we return a customized pull strategy instead of the default one.
    private sealed class InjectionStrategy : DefaultDependencyInjectionStrategy
    {
        public InjectionStrategy( DependencyProperties properties ) : base( properties ) { }

        protected override IPullStrategy GetPullStrategy( IFieldOrProperty introducedFieldOrProperty )
        {
            return new LoggerPullStrategy( this.Properties, introducedFieldOrProperty );
        }
    }

    // Our customized pull strategy. Decides how to assign the field or property from the constructor.
    private sealed class LoggerPullStrategy : DefaultPullStrategy
    {
        public LoggerPullStrategy( DependencyProperties properties, IFieldOrProperty introducedFieldOrProperty ) : base( properties, introducedFieldOrProperty )
        {
            var loggerType = (INamedType) introducedFieldOrProperty.Type;
            var genericLoggerType = loggerType.ContainingNamespace.Types.OfName( "ILogger" ).Single( l => l.TypeParameters.Count == 1 );

            // Sets the type of the required or created constructor parameter. We return ILogger<T> where T is the declaring type.
            // (The default behavior would return just ILogger).
            this.ParameterType = genericLoggerType.WithTypeArguments( this.IntroducedFieldOrProperty.DeclaringType );
        }

        protected override IType ParameterType { get; }
    }
}