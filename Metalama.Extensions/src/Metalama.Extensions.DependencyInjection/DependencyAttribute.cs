// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Custom attribute that, when applied to a field or automatic property in user code, means that this field or property is a service dependency
/// that must be pulled from the dependency injection framework. Contrarily to <see cref="IntroduceDependencyAttribute"/> that must be used
/// in aspect code, this attribute must be used in user code. 
/// </summary>
/// <remarks>
///  The implementation of this custom attribute depends on the selected dependency injection framework.
/// </remarks>
[PublicAPI]
public class DependencyAttribute : FieldOrPropertyAspect
{
    private bool? _isLazy;
    private bool? _isRequired;

    /// <summary>
    /// Gets or sets a value indicating whether the dependency should be pulled from the container lazily, i.e. upon first use.
    /// </summary>
    public bool IsLazy
    {
        get => this._isLazy.GetValueOrDefault();
        set => this._isLazy = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dependency is required.
    /// </summary>
    public bool IsRequired
    {
        get => this._isRequired.GetValueOrDefault();
        set => this._isRequired = value;
    }

    protected virtual DependencyProperties ToProperties( IFieldOrProperty target )
    {
        var isRequired = this._isRequired ?? target.Type.IsNullable switch
        {
            null => null,
            true => false,
            false => true
        };

        return new DependencyProperties(
            target.DeclaringType,
            target.Type,
            target.Name,
            target.IsStatic,
            isRequired,
            this._isLazy,
            target.DeclarationKind );
    }

    public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        var target = builder.Target;

        var dependencyProperties = this.ToProperties( target );

        if ( !dependencyProperties.Options.TryGetFramework( dependencyProperties, builder.Diagnostics, out var framework ) )
        {
            builder.SkipAspect();

            return;
        }

        framework.TryImplementDependency( dependencyProperties, builder );
    }
}