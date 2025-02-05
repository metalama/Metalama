// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects;

/// <summary>
/// Custom attribute attribute that, when applied to a declaration, specifies that this declaration and all its members must not be
/// the target of aspects of given types, unless the aspect is explicitly specified as a custom attribute.
/// </summary>
[AttributeUsage( AttributeTargets.All )]
public sealed class ExcludeAspectAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExcludeAspectAttribute"/> class.
    /// </summary>
    /// <param name="excludedAspectTypes"></param>
    public ExcludeAspectAttribute( params Type[] excludedAspectTypes )
    {
        _ = excludedAspectTypes;
    }

    /// <summary>
    /// Gets or sets the justification of the exclusion.
    /// </summary>
    public string? Justification { get; set; }
}