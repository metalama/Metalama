// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Describes a parameter of a method template, as returned by <see cref="ExtensionTemplateServices.TryGetMethodTemplateParameters"/>.
/// </summary>
[PublicAPI]
public sealed class ExtensionTemplateParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionTemplateParameter"/> class.
    /// </summary>
    internal ExtensionTemplateParameter( string name, bool isCompileTime, IType type, RefKind refKind )
    {
        this.Name = name;
        this.IsCompileTime = isCompileTime;
        this.Type = type;
        this.RefKind = refKind;
    }

    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets a value indicating whether the parameter is a compile-time parameter, which receives a template argument, rather than a run-time
    /// parameter, which binds to a parameter of the method that the template implements.
    /// </summary>
    public bool IsCompileTime { get; }

    /// <summary>
    /// Gets the type of the parameter in the compilation given to <see cref="ExtensionTemplateServices.TryGetMethodTemplateParameters"/>.
    /// </summary>
    public IType Type { get; }

    /// <summary>
    /// Gets the reference kind of the parameter.
    /// </summary>
    public RefKind RefKind { get; }
}
