// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Custom attribute that, when applied to a declaration, prevents specified aspect types from being applied to this declaration
/// and all its members, except when the aspect is explicitly added as a custom attribute on the declaration.
/// </summary>
/// <remarks>
/// <para>
/// This attribute is useful for excluding aspects that would otherwise be applied through:
/// </para>
/// <list type="bullet">
/// <item><description><b>Aspect inheritance:</b> Prevents inherited aspects from propagating to this declaration</description></item>
/// <item><description><b>Fabrics:</b> Prevents fabric-added aspects from targeting this declaration</description></item>
/// <item><description><b>Child aspects:</b> Prevents child aspects from being added to this declaration by other aspects</description></item>
/// </list>
/// <para>
/// Aspects explicitly applied as custom attributes directly on the declaration are not excluded. This allows
/// intentional opt-in for specific declarations while excluding automatic application.
/// </para>
/// </remarks>
/// <seealso href="@aspect-inheritance"/>
/// <seealso href="@same-type-multiple-instances"/>
[AttributeUsage( AttributeTargets.All )]
public sealed class ExcludeAspectAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeAspectAttribute"/> class.
    /// </summary>
    /// <param name="excludedAspectTypes">An array of aspect types to exclude from this declaration and its members.</param>
    public ExcludeAspectAttribute( params Type[] excludedAspectTypes )
    {
        _ = excludedAspectTypes;
    }

    /// <summary>
    /// Gets or sets the justification of the exclusion.
    /// </summary>
    public string? Justification { get; set; }
}