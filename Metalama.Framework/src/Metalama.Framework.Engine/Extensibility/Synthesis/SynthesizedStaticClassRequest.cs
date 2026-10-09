// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes a request to declare a static class with <see cref="CallSites.ExtensionTransformationFactory.DeclareStaticClass"/>.
/// </summary>
[PublicAPI]
public sealed class SynthesizedStaticClassRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedStaticClassRequest"/> class.
    /// </summary>
    /// <param name="nameHint">The name of the class. The factory adds a numeric suffix when the name is already used in the namespace.</param>
    /// <exception cref="ArgumentException"><paramref name="nameHint"/> is not a valid identifier.</exception>
    public SynthesizedStaticClassRequest( string nameHint )
    {
        this.NameHint = SynthesisNames.ValidateIdentifier( nameHint, nameof(nameHint) );
    }

    /// <summary>
    /// Gets the name of the class. The factory adds a numeric suffix when the name is already used in the namespace.
    /// </summary>
    public string NameHint { get; }

    /// <summary>
    /// Gets the full name of the namespace of the class, or <c>null</c> for the global namespace. The parts of the name that do not exist in the
    /// compilation are declared.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Gets the accessibility of the class. The default is <see cref="Accessibility.Internal"/>.
    /// </summary>
    public Accessibility Accessibility { get; init; } = Accessibility.Internal;
}
