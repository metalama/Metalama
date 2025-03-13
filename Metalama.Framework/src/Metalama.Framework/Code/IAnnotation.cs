// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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