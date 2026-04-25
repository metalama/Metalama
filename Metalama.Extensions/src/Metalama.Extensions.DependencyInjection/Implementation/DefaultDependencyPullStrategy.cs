// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Extensions.DependencyInjection.Implementation;

/// <summary>
/// The default implementation of <see cref="IDependencyPullStrategy"/>.
/// </summary>
[PublicAPI]
public class DefaultDependencyPullStrategy : IDependencyPullStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultDependencyPullStrategy"/> class.
    /// </summary>
    /// <param name="properties">The context information for the introduced dependency.</param>
    /// <param name="introducedFieldOrProperty">The dependency field or property in the target type.</param>
    public DefaultDependencyPullStrategy( DependencyProperties properties, IFieldOrProperty introducedFieldOrProperty )
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

    /// <summary>
    /// Gets the type of the constructor parameter. This is used by both <see cref="IParameterPullStrategy.GetNewParameter"/> and <see cref="IParameterPullStrategy.GetExistingParameter"/>.
    /// </summary>
    protected virtual IType ParameterType => this.IntroducedFieldOrProperty.Type.ToNullable();

    /// <inheritdoc />
    public virtual IStatement GetAssignmentStatement( IParameter existingParameter )
        => this.GetAssignmentStatement( existingParameter, this.AssignedFieldOrProperty );

    public IParameterPullStrategy CreateParameterPullStrategy()
        => new DefaultParameterPullStrategy( this.ParameterType.ToRef(), this.IntroducedFieldOrProperty.Name );

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