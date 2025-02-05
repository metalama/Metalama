// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Metrics;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;

namespace Metalama.Framework.Engine.CodeModel.Abstractions;

internal interface IDeclarationImpl : ISdkDeclaration, ICompilationElementImpl, IMeasurableInternal
{
    /// <summary>
    /// Gets the <see cref="Microsoft.CodeAnalysis.SyntaxReference"/> syntaxes that declare the current declaration.
    /// In case of a member introduction, this returns the syntax references of the type.
    /// In case of a type introduction, this returns an empty list.
    /// </summary>
    ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

    /// <summary>
    /// Gets a value indicating whether a declaration can be inherited or overridden.
    /// </summary>
    bool CanBeInherited { get; }

    /// <summary>
    /// Gets a value indicating the syntax tree of the input compilation where the declaration primary resides.
    /// </summary>
    SyntaxTree? PrimarySyntaxTree { get; }

    IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default );

    DeclarationImplementationKind ImplementationKind { get; }
}