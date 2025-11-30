// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Code;

/// <summary>
/// A non-generic base interface for the generic <see cref="IAnnotation{T}"/>. You should always implement the generic interface.
/// </summary>
/// <seealso cref="IAnnotation{T}"/>
/// <seealso cref="DeclarationEnhancements{T}"/>
/// <seealso cref="ICompileTimeSerializable"/>
public interface IAnnotation : ICompileTimeSerializable;

// ReSharper disable once UnusedTypeParameter
/// <summary>
/// Represents an annotation - an arbitrary serializable object that aspects can attach to declarations
/// to communicate information to other aspects or subsequent compilation phases.
/// </summary>
/// <remarks>
/// <para>
/// Annotations provide a mechanism for aspects to store and share custom metadata on declarations.
/// Common use cases include:
/// </para>
/// <list type="bullet">
/// <item><description><b>Inter-aspect communication:</b> One aspect adds an annotation that other aspects can query to coordinate behavior</description></item>
/// <item><description><b>Code generation metadata:</b> Store configuration or contextual information that template code needs during code generation</description></item>
/// <item><description><b>Aspect coordination:</b> Mark declarations with flags or data that influence how subsequent aspects process them</description></item>
/// </list>
/// <para>
/// <b>Adding annotations:</b> Use <see cref="AdviserExtensions.AddAnnotation{TDeclaration}"/> in your aspect's
/// <see cref="IAspect{T}.BuildAspect"/> method to attach an annotation to a declaration.
/// </para>
/// <para>
/// <b>Retrieving annotations:</b> Call <see cref="DeclarationEnhancements{T}.GetAnnotations{TAnnotation}"/>
/// via <see cref="DeclarationExtensions.Enhancements{T}"/> to retrieve annotations from a declaration.
/// </para>
/// <para>
/// <b>Serialization:</b> Annotations must be compile-time serializable (implement <see cref="ICompileTimeSerializable"/>)
/// as they may be persisted and accessed across project boundaries.
/// </para>
/// <para>
/// <b>Design-time limitation:</b> At design time (in the IDE), Metalama performs partial compilations that only include
/// the inheritance closure of modified files. Aspects targeting declarations outside of this partial compilation
/// do not execute, so their annotations are not added and cannot be queried. This limitation does not apply at full
/// compile time when all aspects execute.
/// </para>
/// </remarks>
/// <typeparam name="T">The type of declarations to which the annotation can be added.</typeparam>
/// <seealso cref="IAnnotation"/>
/// <seealso cref="DeclarationEnhancements{T}"/>
/// <seealso cref="AdviserExtensions.AddAnnotation{TDeclaration}"/>
/// <seealso cref="DeclarationExtensions.Enhancements{T}"/>
/// <seealso cref="ICompileTimeSerializable"/>
/// <seealso href="@sharing-state-with-advice"/>
public interface IAnnotation<in T> : IAnnotation
    where T : class, IDeclaration;