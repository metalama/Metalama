// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces;

/// <summary>
/// Represents a set of compilations and exposes lists of declarations that merge the declarations from all
/// the compilations in the set.
/// </summary>
[PublicAPI]
public interface ICompilationSet
{
    /// <summary>
    /// Gets the list of compilations in the current set.
    /// </summary>
    ImmutableArray<ICompilation> Compilations { get; }

    /// <summary>
    /// Gets all types in the current set of compilations, including nested types.
    /// </summary>
    ImmutableArray<INamedType> Types { get; }

    /// <summary>
    /// Gets all methods in the current set of compilations, except local methods.
    /// </summary>
    ImmutableArray<IMethod> Methods { get; }

    /// <summary>
    /// Gets all fields in the current set of compilations.
    /// </summary>
    ImmutableArray<IField> Fields { get; }

    /// <summary>
    /// Gets all properties in the current set of compilations.
    /// </summary>
    ImmutableArray<IProperty> Properties { get; }

    /// <summary>
    /// Gets all properties and properties in the current set of compilations.
    /// </summary>
    ImmutableArray<IFieldOrProperty> FieldsAndProperties { get; }

    /// <summary>
    /// Gets all constructors in the current set of compilations.
    /// </summary>
    ImmutableArray<IConstructor> Constructors { get; }

    /// <summary>
    /// Gets all events in the current set of compilations.
    /// </summary>
    ImmutableArray<IEvent> Events { get; }

    /// <summary>
    /// Gets all target frameworks of projects in the current set of compilations.
    /// </summary>
    ImmutableArray<string> TargetFrameworks { get; }
}