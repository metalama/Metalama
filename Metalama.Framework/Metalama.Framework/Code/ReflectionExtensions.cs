// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Reflection;

namespace Metalama.Framework.Code;

[CompileTime]
[PublicAPI]
public static class ReflectionExtensions
{
    // ReSharper disable once SuspiciousTypeConversion.Global
    
    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="MemberInfo"/>, when available, or <see langword="null"/>.
    /// </summary>
    public static IExpression? AsExpression( this MemberInfo memberInfo ) => memberInfo as IExpression;
    
    // ReSharper disable once SuspiciousTypeConversion.Global
    
    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="ParameterInfo"/>, when available, or <see langword="null"/>.
    /// </summary>
    public static IExpression? AsExpression( this ParameterInfo parameterInfo ) => parameterInfo as IExpression;

    private static IExpression Throw( object? obj )
        => throw new InvalidOperationException(
            $"Cannot convert an instance of type {obj?.GetType().Name} to a run-time expression. If you are attempting to use a run-time expression as IExpression in compile-time code, that is not supported." );

    // ReSharper disable once SuspiciousTypeConversion.Global
    
    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="MemberInfo"/>, when available, or throws an exception.
    /// </summary>
    public static IExpression ToExpression( this MemberInfo memberInfo ) => memberInfo as IExpression ?? Throw( memberInfo );
    
    // ReSharper disable once SuspiciousTypeConversion.Global
    
    /// <summary>
    /// Returns the <see cref="IExpression"/> representation of the given <see cref="ParameterInfo"/>, when available, or throws an exception.
    /// </summary>
    public static IExpression ToExpression( this ParameterInfo parameterInfo ) => parameterInfo as IExpression ?? Throw( parameterInfo );
}