// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Marks a field or automatic property as a service dependency that should be injected by the dependency injection framework.
/// This attribute is used in user code, whereas <see cref="IntroduceDependencyAttribute"/> is used in aspect code to introduce dependencies.
/// </summary>
/// <remarks>
/// <para>
/// When applied to a field or property, this aspect will automatically implement dependency injection according to the
/// configured DI framework (e.g., constructor injection, property injection). The implementation depends on the selected
/// dependency injection framework, which can be configured using <see cref="DependencyInjectionExtensions.ConfigureDependencyInjection(Framework.Fabrics.IQuery{ICompilation}, System.Action{DependencyInjectionOptionsBuilder})"/>.
/// </para>
/// </remarks>
/// <seealso cref="IntroduceDependencyAttribute"/>
/// <seealso cref="DependencyInjectionExtensions"/>
/// <seealso href="@dependency-injection"/>
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