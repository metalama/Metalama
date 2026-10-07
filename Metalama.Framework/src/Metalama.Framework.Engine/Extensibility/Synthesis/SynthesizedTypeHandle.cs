// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Represents a type that a pipeline extension declared with <see cref="CallSites.ExtensionTransformationFactory.DeclareStaticClass"/>.
/// </summary>
[PublicAPI]
public sealed class SynthesizedTypeHandle
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SynthesizedTypeHandle"/> class.
    /// </summary>
    internal SynthesizedTypeHandle( NamedTypeBuilder builder, INamedType type )
    {
        this.Builder = builder;
        this.Type = type;
    }

    /// <summary>
    /// Gets the declared type. It is not visible in the code model to aspects, in any stage of the pipeline, because it is added only to the
    /// compilation that the linker sees.
    /// </summary>
    public INamedType Type { get; }

    /// <summary>
    /// Gets the name of the declared type, including the numeric suffix that the factory may have added.
    /// </summary>
    public string Name => this.Type.Name;

    /// <summary>
    /// Gets the builder of the type.
    /// </summary>
    internal NamedTypeBuilder Builder { get; }
}
