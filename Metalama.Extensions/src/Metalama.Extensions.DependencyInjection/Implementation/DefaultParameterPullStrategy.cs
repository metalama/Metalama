// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using System;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// The default implementation of <see cref="IParameterPullStrategy"/> that handles constructor parameter introduction
/// and propagation for dependency injection.
/// </summary>
/// <remarks>
/// <para>
/// This class is responsible for determining whether an existing constructor parameter can satisfy a dependency,
/// and if not, specifying how to create a new parameter. It also handles parameter propagation to derived classes
/// via the <see cref="IPullStrategy"/> interface.
/// </para>
/// <para>
/// For most use cases, you can use this default implementation. Override the virtual methods if you need custom
/// behavior for parameter matching or creation.
/// </para>
/// </remarks>
/// <seealso cref="IDependencyPullStrategy"/>
/// <seealso cref="DefaultDependencyPullStrategy"/>
/// <seealso cref="DefaultDependencyInjectionStrategy"/>
/// <seealso href="@dependency-injection"/>
[PublicAPI]
public class DefaultParameterPullStrategy : IParameterPullStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultParameterPullStrategy"/> class.
    /// </summary>
    /// <param name="parameterType">A reference to the type of the constructor parameter.</param>
    /// <param name="dependencyName">The name of the dependency, used to derive the parameter name.</param>
    public DefaultParameterPullStrategy( IRef<IType> parameterType, string dependencyName )
    {
        this.ParameterType = parameterType;
        this.DependencyName = dependencyName;
    }

    /// <summary>
    /// Gets the type of the constructor parameter.
    /// </summary>
    public IRef<IType> ParameterType
    {
        get;

        [UsedImplicitly]
        private set;
    }

    /// <summary>
    /// Gets the name of the dependency, which is used to derive the name of the constructor parameter.
    /// </summary>
    public string DependencyName
    {
        get;

        [UsedImplicitly]
        private set;
    }

    /// <inheritdoc />
    public virtual IParameter? GetExistingParameter( IConstructor constructor )
    {
        var parameterType = this.ParameterType.GetTarget( constructor.Compilation );

        return constructor.Parameters.FirstOrDefault( p => p.Type.IsConvertibleTo( parameterType ) );
    }

    /// <inheritdoc />
    public virtual ParameterSpecification GetNewParameter( IConstructor constructor )
    {
        var parameterName = this.GetNewParameterName( constructor );
        var parameterType = this.ParameterType.GetTarget( constructor.Compilation );

        return new ParameterSpecification( parameterName, parameterType );
    }

    /// <inheritdoc />
    public virtual PullAction GetPullAction( IParameter pulledParameter, IHasParameters targetMember )
    {
        var callingConstructor = (IConstructor) targetMember;

        var existingParameter = this.GetExistingParameter( callingConstructor );

        if ( existingParameter != null )
        {
            return PullAction.UseExistingParameter( existingParameter );
        }
        else
        {
            var newParameter = this.GetNewParameter( callingConstructor );

            return PullAction.IntroduceParameterAndPull(
                newParameter.Name,
                newParameter.Type,
                parameterDefaultValue: TypedConstant.Default( newParameter.Type, newParameter.Type.IsNullable == false ),
                parameterAttributes: newParameter.Attributes );
        }
    }

    /// <summary>
    /// Gets the name of the new constructor parameter.
    /// </summary>
    private string GetNewParameterName( IConstructor constructor )
    {
        // Apply naming conventions.
        var parameterName = CleanParameterName( this.DependencyName );

        // Deduplicate against source-defined parameters only. Aspect-introduced parameters
        // with the same name should not be avoided — IntroduceParameter will detect them and
        // replace their type if the new type is more specific (e.g. ILogger<Derived> replacing
        // ILogger<Base>). If we dedup here, IntroduceParameter never sees the name match and
        // creates a duplicate.
        var deduplicate = 0;

        while ( constructor.Parameters.Any( p => p.Name == parameterName && p.Origin.Kind == DeclarationOriginKind.Source ) )
        {
            deduplicate++;
            parameterName = $"{parameterName}{deduplicate}";
        }

        return parameterName;
    }

    /// <summary>
    /// Normalizes the name of the parameter by applying naming conventions.
    /// </summary>
    /// <param name="parameterName">The input parameter name.</param>
    /// <returns>The normalized parameter name.</returns>
    private static string CleanParameterName( string parameterName )
    {
        // Take the parameter name from the name of the field or property.
        parameterName = parameterName.TrimStart( '_' );

        if ( parameterName.Length == 0 )
        {
            throw new InvalidOperationException( "The name of the field or property cannot be only underscores." );
        }

        return parameterName[0].ToString().ToLowerInvariant() + parameterName.Substring( 1 );
    }
}