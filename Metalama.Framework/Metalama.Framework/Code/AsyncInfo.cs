// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code;

/// <summary>
/// Information about an async method, returned by the <see cref="MethodExtensions.GetAsyncInfo"/> extension method of <see cref="IMethod"/>.
/// </summary>
[CompileTime]
public readonly struct AsyncInfo
{
    /// <summary>
    /// Gets a value indicating whether the method has an async implementation, i.e. has the <c>async</c> modifier.
    /// </summary>
    public bool? IsAsync { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method is awaitable, i.e. whether it can be used with the <c>await</c> keyword.
    /// This does not include <c>IAsyncEnumerable</c>, which can be used with <c>await foreach</c> but not <c>await</c>.
    /// </summary>
    public bool IsAwaitable { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method has an <c>AsyncMethodBuilderAttribute</c> custom attribute.
    /// </summary>
    public bool HasMethodBuilder { get; }

    /// <summary>
    /// Gets a value indicating whether the return type of the method is either awaitable (see <see cref="IsAwaitable"/>) either <c>void</c>.
    /// </summary>
    public bool IsAwaitableOrVoid => this.IsAwaitable || this.ResultType.Equals( SpecialType.Void );

    /// <summary>
    /// Gets the type of the result of the async method, i.e. the type of the <c>await</c> expression.
    /// </summary>
    public IType ResultType { get; }

    internal AsyncInfo( bool? isAsync, bool isAwaitable, IType resultType, bool hasMethodBuilder )
    {
        this.IsAsync = isAsync;
        this.IsAwaitable = isAwaitable;
        this.ResultType = resultType;
        this.HasMethodBuilder = hasMethodBuilder;
    }
}