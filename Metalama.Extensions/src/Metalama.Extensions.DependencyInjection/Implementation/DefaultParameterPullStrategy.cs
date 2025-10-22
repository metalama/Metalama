// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using System;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

public class DefaultParameterPullStrategy : IParameterPullStrategy
{
    public DefaultParameterPullStrategy( IRef<IType> parameterType, string dependencyName )
    {
        this.ParameterType = parameterType;
        this.DependencyName = dependencyName;
    }

    public IRef<IType> ParameterType { get; private set; }

    public string DependencyName { get; private set; }

    public virtual IParameter? GetExistingParameter( IConstructor constructor )
    {
        var parameterType = this.ParameterType.GetTarget( constructor.Compilation );

        return constructor.Parameters.FirstOrDefault( p => TypeExtensions.IsConvertibleTo( p.Type, parameterType ) );
    }

    public virtual ParameterSpecification GetNewParameter( IConstructor constructor )
    {
        var parameterName = this.GetNewParameterName( constructor );
        var parameterType = this.ParameterType.GetTarget( constructor.Compilation );

        return new ParameterSpecification( parameterName, parameterType );
    }

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
                TypedConstant.Default( newParameter.Type ),
                newParameter.Attributes );
        }
    }

    /// <summary>
    /// Gets the name of the new constructor parameter.
    /// </summary>
    private string GetNewParameterName( IConstructor constructor )
    {
        // Apply naming conventions.
        var parameterName = CleanParameterName( this.DependencyName );

        // Deduplicate.
        var deduplicate = 0;

        while ( constructor.Parameters.OfName( parameterName ) != null )
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