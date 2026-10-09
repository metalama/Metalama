// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Extensibility.Synthesis;

/// <summary>
/// Describes the expression that <c>meta.Proceed()</c> produces in a method that a pipeline extension declares from a template with
/// <see cref="CallSites.ExtensionTransformationFactory.DeclareMethod"/>.
/// </summary>
/// <remarks>
/// <para>
/// The arguments of the invocation are parameters of the declared method. By default, the parameter <c>i</c> of the invoked method receives the
/// parameter <c>i</c> of the declared method, after the receiver parameter for <see cref="ProceedBindingKind.InvokeOnParameter"/>. Each argument
/// is passed with the <c>ref</c>, <c>out</c> or <c>in</c> modifier of the parameter of the invoked method.
/// </para>
/// <para>
/// The invocation keeps virtual and interface dispatch, because it is written as an ordinary call of the method.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class ProceedBinding
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProceedBinding"/> class. Use the static members of the class to create an instance.
    /// </summary>
    private ProceedBinding(
        ProceedBindingKind kind,
        IMethod method,
        int receiverParameterIndex,
        ImmutableArray<IType> typeArguments,
        ImmutableArray<int> argumentParameterIndices,
        ImmutableArray<(int ParameterIndex, IType Type)> argumentCasts )
    {
        this.Kind = kind;
        this.Method = method;
        this.ReceiverParameterIndex = receiverParameterIndex;
        this.TypeArguments = typeArguments.IsDefault ? ImmutableArray<IType>.Empty : typeArguments;
        this.ArgumentParameterIndices = argumentParameterIndices;
        this.ArgumentCasts = argumentCasts.IsDefault ? ImmutableArray<(int ParameterIndex, IType Type)>.Empty : argumentCasts;
    }

    /// <summary>
    /// Creates a binding that invokes a static method.
    /// </summary>
    /// <param name="method">The invoked method. A method of a constructed generic type is written with the type arguments of that type.</param>
    /// <param name="typeArguments">The type arguments written in the invocation, or an empty or default array to write none.</param>
    /// <param name="argumentParameterIndices">For each parameter of <paramref name="method"/>, the index of the parameter of the declared method
    /// that is passed. The default is the identity.</param>
    public static ProceedBinding InvokeStatic(
        IMethod method,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default )
    {
        if ( method == null )
        {
            throw new ArgumentNullException( nameof(method) );
        }

        if ( !method.IsStatic )
        {
            throw new ArgumentException( $"The method '{method}' must be static.", nameof(method) );
        }

        return new ProceedBinding( ProceedBindingKind.InvokeStatic, method, -1, typeArguments, argumentParameterIndices, default );
    }

    /// <summary>
    /// Creates a binding that invokes an instance method on a parameter of the declared method.
    /// </summary>
    /// <param name="method">The invoked method.</param>
    /// <param name="receiverParameterIndex">The index of the parameter of the declared method that is the receiver of the invocation.</param>
    /// <param name="typeArguments">The type arguments written in the invocation, or an empty or default array to write none.</param>
    /// <param name="argumentParameterIndices">For each parameter of <paramref name="method"/>, the index of the parameter of the declared method
    /// that is passed. The default passes the parameters of the declared method in order, skipping the receiver parameter.</param>
    public static ProceedBinding InvokeOnParameter(
        IMethod method,
        int receiverParameterIndex,
        ImmutableArray<IType> typeArguments = default,
        ImmutableArray<int> argumentParameterIndices = default )
    {
        if ( method == null )
        {
            throw new ArgumentNullException( nameof(method) );
        }

        if ( method.IsStatic )
        {
            throw new ArgumentException( $"The method '{method}' must not be static.", nameof(method) );
        }

        if ( receiverParameterIndex < 0 )
        {
            throw new ArgumentOutOfRangeException( nameof(receiverParameterIndex) );
        }

        return new ProceedBinding(
            ProceedBindingKind.InvokeOnParameter,
            method,
            receiverParameterIndex,
            typeArguments,
            argumentParameterIndices,
            default );
    }

    /// <summary>
    /// Returns a copy of the current binding that casts some arguments to a given type, for instance a parameter that the declared method widens to
    /// <c>object</c> or declares as <c>dynamic</c>.
    /// </summary>
    /// <param name="casts">The index of each parameter of the declared method to cast, and the type to which it is cast.</param>
    public ProceedBinding WithArgumentCasts( ImmutableArray<(int ParameterIndex, IType Type)> casts )
        => new( this.Kind, this.Method, this.ReceiverParameterIndex, this.TypeArguments, this.ArgumentParameterIndices, casts );

    /// <summary>
    /// Gets the kind of binding.
    /// </summary>
    public ProceedBindingKind Kind { get; }

    /// <summary>
    /// Gets the invoked method.
    /// </summary>
    public IMethod Method { get; }

    /// <summary>
    /// Gets the index of the parameter of the declared method that is the receiver of the invocation, or -1 for a static method.
    /// </summary>
    public int ReceiverParameterIndex { get; }

    /// <summary>
    /// Gets the type arguments written in the invocation.
    /// </summary>
    public ImmutableArray<IType> TypeArguments { get; }

    /// <summary>
    /// Gets, for each parameter of <see cref="Method"/>, the index of the parameter of the declared method that is passed, or a default array for
    /// the default mapping.
    /// </summary>
    public ImmutableArray<int> ArgumentParameterIndices { get; }

    /// <summary>
    /// Gets the arguments that are cast, as the index of a parameter of the declared method and the type to which it is cast.
    /// </summary>
    public ImmutableArray<(int ParameterIndex, IType Type)> ArgumentCasts { get; }
}
