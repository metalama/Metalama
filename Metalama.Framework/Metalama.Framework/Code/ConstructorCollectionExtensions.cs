// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code;

/// <summary>
/// Provides extension methods to the <see cref="IConstructorCollection"/> interface.
/// </summary>
[CompileTime]
public static class ConstructorCollectionExtensions
{
    /// <summary>
    /// Gets a list of constructors with signatures compatible with specified constraints given using the <c>System.Reflection</c> API.
    /// </summary>
    /// <param name="constructors">A collection of constructors.</param>
    /// <param name="argumentTypes">Constraint on reflection types of arguments. <c>Null</c>items in the list signify any type.</param>
    /// <returns>Enumeration of constructors matching specified constraints.</returns>
    public static IEnumerable<IConstructor> OfCompatibleSignature( this IConstructorCollection constructors, IReadOnlyList<Type?>? argumentTypes )
    {
        return constructors.OfCompatibleSignature(
            (argumentTypes, constructors.DeclaringType.Compilation),
            null,
            argumentTypes?.Count,
            GetParameter,
            false );

        static (IType? Type, RefKind? RefKind) GetParameter( (IReadOnlyList<Type?>? ArgumentTypes, ICompilation Compilation) context, int index )
            => context.ArgumentTypes?[index] != null
                ? (TypeFactory.GetType( context.ArgumentTypes[index]! ), null)
                : (null, null);
    }

    /// <summary>
    /// Gets a list of constructors with signatures compatible with specified constraints given using the Metalama API.
    /// </summary>
    /// <param name="constructors">A collection of constructors.</param> 
    /// <param name="argumentTypes">Constraint on types of arguments. <c>Null</c>items in the list signify any type.</param>
    /// <param name="refKinds">Constraint on reference kinds of arguments. <c>Null</c>items in the list signify any reference kind.</param>
    /// <returns>Enumeration of constructors matching specified constraints.</returns>
    public static IEnumerable<IConstructor> OfCompatibleSignature(
        this IConstructorCollection constructors,
        IReadOnlyList<IType?>? argumentTypes = null,
        IReadOnlyList<RefKind?>? refKinds = null )
    {
        return constructors.OfCompatibleSignature( (argumentTypes, refKinds), null, argumentTypes?.Count, GetParameter, false );

        static (IType? Type, RefKind? RefKind) GetParameter( (IReadOnlyList<IType?>? ArgumentTypes, IReadOnlyList<RefKind?>? RefKinds) context, int index )
            => (context.ArgumentTypes?[index], context.RefKinds?[index]);
    }

    /// <summary>
    /// Gets a constructor that exactly matches the specified signature given using the <c>System.Reflection</c> API.
    /// </summary>
    /// <param name="constructors">A collection of constructors.</param>
    /// <param name="parameterTypes">List of parameter types.</param>
    /// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
    /// <returns>A <see cref="IConstructor"/> that matches the given signature.</returns>
    public static IConstructor? OfExactSignature(
        this IConstructorCollection constructors,
        IReadOnlyList<IType> parameterTypes,
        IReadOnlyList<RefKind>? refKinds = null )
    {
        return constructors.OfExactSignature( (parameterTypes, refKinds), null, parameterTypes.Count, GetParameter, false );

        static (IType Type, RefKind RefKind) GetParameter( (IReadOnlyList<IType> ParameterTypes, IReadOnlyList<RefKind>? RefKinds) context, int index )
            => (context.ParameterTypes[index], context.RefKinds?[index] ?? RefKind.None);
    }

    // TODO: add this method
    // IConstructor? OfExactSignature( IReadOnlyList<Type> parameterTypes );

    /// <summary>
    /// Gets a constructor that exactly matches the signature of the specified method.
    /// </summary>
    /// <param name="constructors">A collection of constructors.</param>
    /// <param name="signatureTemplate">Constructor signature of which to should be considered.</param>
    /// <returns>A <see cref="IConstructor"/> that matches the given signature.</returns>
    public static IConstructor? OfExactSignature( this IConstructorCollection constructors, IConstructor signatureTemplate )
    {
        return constructors.OfExactSignature( signatureTemplate, null, signatureTemplate.Parameters.Count, GetParameter, false );

        static (IType Type, RefKind RefKind) GetParameter( IConstructor context, int index )
            => (context.Parameters[index].Type, context.Parameters[index].RefKind);
    }
}