// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Extensions.DependencyInjection.Implementation;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.DependencyInjection;

/// <summary>
/// Custom attribute that, when be applied to a field or automatic property of an aspect, means that this field or property is a service dependency
/// that introduced into the target type and handled by a dependency injection framework. Contrarily to <see cref="DependencyAttribute"/> that can be used
/// in user code, this attribute can be used only in an aspect. 
/// </summary>
/// <remarks>
///  The implementation of this custom attribute depends on the selected dependency injection framework.
/// </remarks>
[PublicAPI]
public class IntroduceDependencyAttribute : DeclarativeAdviceAttribute
{
    private bool? _isLazy;
    private bool? _isRequired;

    protected virtual DependencyOptions ToOptions( IFieldOrProperty templateFieldOrProperty )
    {
        return new DependencyOptions
        {
            IsLazy = this._isLazy,
            IsRequired = this._isRequired,
            IsStatic = templateFieldOrProperty.IsStatic,
            MemberKind = templateFieldOrProperty.DeclarationKind,
            MemberName = templateFieldOrProperty.Name
        };
    }

    public sealed override void BuildAdvice( IMemberOrNamedType templateMember, string templateMemberId, IAspectBuilder<IDeclaration> builder )
    {
        // Suppress warnings on the aspect field.
        builder.Diagnostics.Suppress( DiagnosticDescriptors.NonNullableFieldMustContainValue, templateMember );
        builder.Diagnostics.Suppress( DiagnosticDescriptors.PrivateMemberIsUnused, templateMember );

        var templateFieldOrProperty = (IFieldOrProperty) templateMember;

        var targetType = builder.Target.GetClosestNamedType();

        if ( targetType == null )
        {
            builder.Diagnostics.Report( DiagnosticDescriptors.AdviceUsedInNonTypeContext.WithArguments( builder.Target ) );
            return;
        }

        builder.With( targetType ).IntroduceDependency( templateFieldOrProperty.Type, this.ToOptions( templateFieldOrProperty ) );
    }

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
}