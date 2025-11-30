// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine.CodeModel;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Services;

/// <summary>
/// Provides compilation-scoped services for aspect weavers, including access to the Roslyn compilation
/// and reflection mapping services.
/// </summary>
/// <remarks>
/// <para>
/// Access this interface through <see cref="AspectWeavers.AspectWeaverContext.CompilationServices"/>.
/// </para>
/// </remarks>
/// <seealso cref="IReflectionMapper"/>
/// <seealso cref="AspectWeavers.AspectWeaverContext"/>
[PublicAPI]
public interface ICompilationServices
{
    /// <summary>
    /// Gets the Roslyn <see cref="Compilation"/> associated with this service provider.
    /// </summary>
    Compilation Compilation { get; }

    /// <summary>
    /// Gets a service able to map a .NET reflection <see cref="Type"/> to a Roslyn <see cref="ITypeSymbol"/>.
    /// </summary>
    IReflectionMapper ReflectionMapper { get; }
}