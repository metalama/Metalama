// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

/// <summary>
/// Represents the result of the processing of a compilation by Metalama.
/// </summary>
[PublicAPI]
public interface IIntrospectionCompilationResult : IIntrospectionCompilationDetails
{
    string Name { get; }

    /// <summary>
    /// Gets the resulting compilation.
    /// </summary>
    ICompilation TransformedCode { get; }
}