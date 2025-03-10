// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code.DeclarationBuilders;

/// <summary>
/// Allows to complete the construction of a parameter that has been created using e.g.
/// <see cref="IMethodBaseBuilder.AddParameter(string,IType,Code.RefKind,TypedConstant?)"/>.
/// </summary>
public interface IParameterBuilder : IParameter, IDeclarationBuilder
{
    /// <remarks>
    /// Gets or sets the default value of the parameter, or  <c>default</c> if the parameter type is a struct and the default
    /// value of the parameter is the default value of the struct type.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">The parameter has no default value.</exception>
    new TypedConstant? DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    new IType Type { get; set; }

    /// <summary>
    /// Gets or sets the parameter ref kind.
    /// </summary>
    new RefKind RefKind { get; set; }

    /// <summary>
    /// Gets or sets of the parameter name.
    /// </summary>
    new string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter has the <c>params</c> modifier.
    /// </summary>
    new bool IsParams { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter has the <see langword="this" /> modifier, meaning that the containing method is an extension method.
    /// </summary>
    /// <remarks>
    /// For templates, you can also use <see cref="ThisAttribute"/>.
    /// </remarks>
    new bool IsThis { get; set; }
}