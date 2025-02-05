// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.SyntaxBuilders;
using System;
using System.Linq;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// The default implementation of <see cref="IPullStrategy"/>.
/// </summary>
public class DefaultPullStrategy : IPullStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPullStrategy"/> class.
    /// </summary>
    /// <param name="properties">The context information for the introduced dependency.</param>
    /// <param name="introducedFieldOrProperty">The dependency field or property in the target type.</param>
    public DefaultPullStrategy( DependencyProperties properties, IFieldOrProperty introducedFieldOrProperty )
    {
        this.Properties = properties;
        this.IntroducedFieldOrProperty = introducedFieldOrProperty;
    }

    /// <summary>
    /// Gets the <see cref="DependencyProperties"/>.
    /// </summary>
    private DependencyProperties Properties { get; }

    /// <summary>
    /// Gets the dependency field or property in the target type. 
    /// </summary>
    protected IFieldOrProperty IntroducedFieldOrProperty { get; }

    /// <summary>
    /// Gets the field or property that must be assigned by the <see cref="GetAssignmentStatement(IParameter)"/> method.
    /// </summary>
    protected virtual IFieldOrProperty AssignedFieldOrProperty => this.IntroducedFieldOrProperty;

    /// <inheritdoc />
    public virtual IParameter? GetExistingParameter( IConstructor constructor )
        => constructor.Parameters.FirstOrDefault( p => p.Type.IsConvertibleTo( this.ParameterType ) );

    /// <summary>
    /// Gets the name of the new constructor parameter.
    /// </summary>
    private string GetNewParameterName( IConstructor constructor )
    {
        // Apply naming conventions.
        var parameterName = CleanParameterName( this.IntroducedFieldOrProperty.Name );

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
    /// Gets the type of the constructor parameter. This is used by both <see cref="GetNewParameter"/> and <see cref="GetExistingParameter"/>.
    /// </summary>
    protected virtual IType ParameterType => this.IntroducedFieldOrProperty.Type.ToNullable();

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

    /// <inheritdoc />
    public virtual ParameterSpecification GetNewParameter( IConstructor constructor )
    {
        var parameterName = this.GetNewParameterName( constructor );

        return new ParameterSpecification( parameterName, this.ParameterType );
    }

    /// <inheritdoc />
    public virtual PullAction PullParameter( IParameter calledConstructorParameter, IConstructor callingConstructor )
    {
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

    /// <inheritdoc />
    public virtual IStatement GetAssignmentStatement( IParameter existingParameter )
        => this.GetAssignmentStatement( existingParameter, this.AssignedFieldOrProperty );

    private IStatement GetAssignmentStatement( IParameter existingParameter, IFieldOrProperty assignedFieldOrProperty )
    {
        // Initialize the field or property to the parameter.
        string assignmentCode;

        if ( this.Properties.IsRequired )
        {
            assignmentCode =
                $"this.{assignedFieldOrProperty.Name} = {existingParameter.Name} ?? throw new System.ArgumentNullException(nameof({existingParameter.Name}));";
        }
        else
        {
            assignmentCode = $"this.{assignedFieldOrProperty.Name} = {existingParameter.Name};";
        }

        return StatementFactory.Parse( assignmentCode );
    }
}