// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel;

/// <summary>
/// Provides a service to map .NET reflection types to Roslyn symbols.
/// </summary>
/// <seealso cref="ICompilationServices"/>
[PublicAPI]
public interface IReflectionMapper
{
    /// <summary>
    /// Gets the Roslyn <see cref="ITypeSymbol"/> corresponding to a .NET reflection <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The .NET reflection type.</param>
    /// <returns>The corresponding Roslyn type symbol.</returns>
    ITypeSymbol GetTypeSymbol( Type type );
}