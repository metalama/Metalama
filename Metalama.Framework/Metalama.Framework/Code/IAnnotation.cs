// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Code;

/// <summary>
/// A non-generic base interface for the generic <see cref="IAnnotation{T}"/>. You should always implement the generic interface.
/// </summary>
public interface IAnnotation : ICompileTimeSerializable;

// ReSharper disable once UnusedTypeParameter
/// <summary>
/// An annotation is an arbitrary but serializable object that can then be retrieved
/// using the <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/> method of the <see cref="DeclarationExtensions.Enhancements{T}"/> object.
/// Annotations are a way of communication between aspects or classes of aspects.
/// </summary>
/// <typeparam name="T">The type of declarations to which the annotation can be added.</typeparam>
public interface IAnnotation<in T> : IAnnotation
    where T : class, IDeclaration;