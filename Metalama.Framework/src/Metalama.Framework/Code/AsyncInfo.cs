// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides information about the async characteristics of a method, returned by the <see cref="MethodExtensions.GetAsyncInfo"/> extension method of <see cref="IMethod"/>.
/// </summary>
/// <remarks>
/// <para>Use this struct to inspect async method properties at compile time, for example in <c>BuildAspect</c>.</para>
/// <para>The <see cref="ResultType"/> property is particularly useful when you need the actual result type instead of the task type.
/// For example, for a method returning <c>Task&lt;string&gt;</c>, <see cref="ResultType"/> is <c>string</c>.</para>
/// </remarks>
/// <seealso cref="MethodExtensions.GetAsyncInfo"/>
/// <seealso cref="IteratorInfo"/>
/// <seealso href="@overriding-methods#async-iterator-default-template"/>
[CompileTime]
public readonly struct AsyncInfo
{
    /// <summary>
    /// Gets a value indicating whether the method has an async implementation, i.e., whether it has the <c>async</c> modifier.
    /// </summary>
    /// <value>
    /// <c>true</c> if the method has the <c>async</c> modifier; <c>false</c> if it does not;
    /// <c>null</c> if the method is not defined in the current project and the information is not available.
    /// </value>
    public bool? IsAsync { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method is awaitable, i.e., whether it can be used with the <c>await</c> keyword.
    /// </summary>
    /// <value>
    /// <c>true</c> if the return type can be used with <c>await</c>; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This does not include <c>IAsyncEnumerable&lt;T&gt;</c>, which can be used with <c>await foreach</c> but not <c>await</c>.
    /// </remarks>
    public bool IsAwaitable { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method has an <c>AsyncMethodBuilderAttribute</c> custom attribute.
    /// </summary>
    /// <value>
    /// <c>true</c> if the return type has an <c>AsyncMethodBuilderAttribute</c>; otherwise, <c>false</c>.
    /// </value>
    public bool HasMethodBuilder { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method is either awaitable (see <see cref="IsAwaitable"/>) or <c>void</c>.
    /// </summary>
    /// <value>
    /// <c>true</c> if the return type is awaitable or <c>void</c>; otherwise, <c>false</c>.
    /// </value>
    public bool IsAwaitableOrVoid => this.IsAwaitable || this.ResultType.Equals( SpecialType.Void );

    /// <summary>
    /// Gets the unwrapped result type of the async method, i.e., the type of the <c>await</c> expression.
    /// </summary>
    /// <value>
    /// The unwrapped result type. For example, for a method returning <c>Task&lt;string&gt;</c>, this property returns <c>string</c>.
    /// For a method returning <c>Task</c> or <c>void</c>, this property returns <c>void</c>.
    /// </value>
    public IType ResultType { get; }

    internal AsyncInfo( bool? isAsync, bool isAwaitable, IType resultType, bool hasMethodBuilder )
    {
        this.IsAsync = isAsync;
        this.IsAwaitable = isAwaitable;
        this.ResultType = resultType;
        this.HasMethodBuilder = hasMethodBuilder;
    }
}