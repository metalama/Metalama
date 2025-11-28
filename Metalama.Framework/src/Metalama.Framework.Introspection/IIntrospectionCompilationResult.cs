// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents the result of the processing of a compilation by Metalama.
/// </summary>
/// <seealso cref="IIntrospectionCompilationDetails"/>
/// <seealso cref="ICompilation"/>
/// <seealso href="@introspection-api"/>
[PublicAPI]
public interface IIntrospectionCompilationResult : IIntrospectionCompilationDetails
{
    /// <summary>
    /// Gets the name of the compilation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the resulting compilation.
    /// </summary>
    ICompilation TransformedCode { get; }
}