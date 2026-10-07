// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Represents a method that a pipeline extension declared with <see cref="CallSites.ExtensionTransformationFactory.DeclareMethod"/>.
/// </summary>
[PublicAPI]
public sealed class SynthesizedMethodHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedMethodHandle"/> class.
    /// </summary>
    internal SynthesizedMethodHandle( IMethod method, SynthesizedMethodBodyTransformation bodyTransformation )
    {
        this.Method = method;
        this.BodyTransformation = bodyTransformation;
    }

    /// <summary>
    /// Gets the declared method. It is not part of the code model that aspects observe.
    /// </summary>
    public IMethod Method { get; }

    /// <summary>
    /// Gets the name of the declared method, including the numeric suffix that the factory may have added.
    /// </summary>
    public string Name => this.Method.Name;

    /// <summary>
    /// Gets the transformation that generates the body of the method from the template.
    /// </summary>
    internal SynthesizedMethodBodyTransformation BodyTransformation { get; }
}
