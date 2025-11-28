// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Reflection;

namespace Metalama.Framework.Code;

/// <summary>
/// Extension methods for reflection types to convert them to Metalama expressions.
/// </summary>
[CompileTime]
[PublicAPI]
[Obsolete]
public static class ReflectionExtensions
{
    // ReSharper disable once SuspiciousTypeConversion.Global

    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="MemberInfo"/>, when available, or <see langword="null"/>.
    /// </summary>
    /// <param name="memberInfo">The member info to convert.</param>
    /// <returns>An <see cref="IExpression"/> if the conversion is possible; otherwise, <c>null</c>.</returns>
    public static IExpression? AsExpression( this MemberInfo memberInfo ) => memberInfo as IExpression;

    // ReSharper disable once SuspiciousTypeConversion.Global

    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="ParameterInfo"/>, when available, or <see langword="null"/>.
    /// </summary>
    /// <param name="parameterInfo">The parameter info to convert.</param>
    /// <returns>An <see cref="IExpression"/> if the conversion is possible; otherwise, <c>null</c>.</returns>
    public static IExpression? AsExpression( this ParameterInfo parameterInfo ) => parameterInfo as IExpression;

    private static IExpression Throw( object? obj )
        => throw new InvalidOperationException(
            $"Cannot convert an instance of type {obj?.GetType().Name} to a run-time expression. If you are attempting to use a run-time expression as IExpression in compile-time code, that is not supported." );

    // ReSharper disable once SuspiciousTypeConversion.Global

    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="MemberInfo"/>, when available, or throws an exception.
    /// </summary>
    /// <param name="memberInfo">The member info to convert.</param>
    /// <returns>An <see cref="IExpression"/> representing the member.</returns>
    /// <exception cref="InvalidOperationException">The member cannot be converted to an expression.</exception>
    public static IExpression ToExpression( this MemberInfo memberInfo ) => memberInfo as IExpression ?? Throw( memberInfo );

    // ReSharper disable once SuspiciousTypeConversion.Global

    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="ParameterInfo"/>, when available, or throws an exception.
    /// </summary>
    /// <param name="parameterInfo">The parameter info to convert.</param>
    /// <returns>An <see cref="IExpression"/> representing the parameter.</returns>
    /// <exception cref="InvalidOperationException">The parameter cannot be converted to an expression.</exception>
    public static IExpression ToExpression( this ParameterInfo parameterInfo ) => parameterInfo as IExpression ?? Throw( parameterInfo );
}