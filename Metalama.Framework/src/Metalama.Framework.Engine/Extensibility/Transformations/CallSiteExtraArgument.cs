// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Represents an argument appended to the rewritten call as a named argument, for example a caller-information value computed from the source
/// call site.
/// </summary>
/// <param name="ParameterName">The name of the parameter of the new target.</param>
/// <param name="Value">The expression of the argument. It is emitted at the call site, after every value of the source call site.</param>
[PublicAPI]
public readonly record struct CallSiteExtraArgument( string ParameterName, ExpressionSyntax Value );
