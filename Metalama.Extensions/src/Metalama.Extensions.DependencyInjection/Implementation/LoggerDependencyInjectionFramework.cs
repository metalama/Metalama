// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// A specialized <see cref="IDependencyInjectionFramework"/> implementation that handles <c>Microsoft.Extensions.Logging.ILogger</c>
/// dependencies by generating the correct code pattern for .NET Core logging.
/// </summary>
/// <remarks>
/// <para>
/// Unlike the standard dependency injection pattern where the constructor parameter type matches the dependency type,
/// the <c>ILogger</c> service requires a parameter of type <c>ILogger&lt;T&gt;</c>, where <c>T</c> is the current type
/// used as a logging category.
/// </para>
/// <para>
/// This framework is automatically registered with a higher priority than <see cref="DefaultDependencyInjectionFramework"/>
/// and only handles dependencies of type <c>Microsoft.Extensions.Logging.ILogger</c>.
/// </para>
/// </remarks>
/// <seealso cref="DefaultDependencyInjectionFramework"/>
/// <seealso cref="IDependencyInjectionFramework"/>
/// <seealso href="@dependency-injection"/>
public sealed class LoggerDependencyInjectionFramework : DefaultDependencyInjectionFramework
{
    /// <inheritdoc />
    public override bool CanHandleDependency( DependencyProperties properties, in ScopedDiagnosticSink diagnostics )
        => properties.DependencyType is INamedType { Name: "ILogger", FullName: "Microsoft.Extensions.Logging.ILogger" };

    /// <inheritdoc />
    protected override DefaultDependencyInjectionStrategy GetStrategy( DependencyProperties properties ) => new InjectionStrategy( properties );

    // Our customized injection strategy. Decides how to create the field or property.
    // We actually have no customization except that we return a customized pull strategy instead of the default one.
    private sealed class InjectionStrategy : DefaultDependencyInjectionStrategy
    {
        public InjectionStrategy( DependencyProperties properties ) : base( properties ) { }

        protected override IDependencyPullStrategy GetDependencyPullStrategy( IFieldOrProperty introducedFieldOrProperty )
        {
            return new LoggerInjectionStrategy( this.Properties, introducedFieldOrProperty );
        }
    }

    // Our customized pull strategy. Decides how to assign the field or property from the constructor.
    private sealed class LoggerInjectionStrategy : DefaultDependencyPullStrategy
    {
        public LoggerInjectionStrategy( DependencyProperties properties, IFieldOrProperty introducedFieldOrProperty ) : base(
            properties,
            introducedFieldOrProperty )
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