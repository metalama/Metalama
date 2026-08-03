// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel
{
    /// <summary>
    /// Represents a subset of a Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>. The subset is limited
    /// to specific syntax trees.
    /// </summary>
    [PublicAPI]
    public abstract partial class PartialCompilation : IPartialCompilationInternal
    {
        public DerivedTypeIndex DerivedTypes => this.LazyDerivedTypes.Value;

        internal Lazy<DerivedTypeIndex> LazyDerivedTypes { get; }

        /// <summary>
        /// Gets the set of modifications present in the current compilation compared to the <see cref="InitialCompilation"/>.
        /// The key of the dictionary is the <see cref="SyntaxTree.FilePath"/> and the value is a <see cref="SyntaxTree"/>
        /// of <see cref="Compilation"/>. 
        /// </summary>
        public ImmutableDictionary<string, SyntaxTreeTransformation> ModifiedSyntaxTrees { get; }

        /// <summary>
        /// Gets the Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>.
        /// </summary>
        public Compilation Compilation => this.CompilationContext.Compilation;

        public CompilationContext CompilationContext { get; }

        /// <summary>
        /// Gets the list of syntax trees in the current subset indexed by path.
        /// </summary>
        [Obsolete( "Use SyntaxTreeCollection to enumerate the syntax trees, or TryGetSyntaxTree to find one by its DocumentKey." )]
        public abstract ImmutableDictionary<string, SyntaxTree> SyntaxTrees { get; }

        /// <inheritdoc cref="IPartialCompilation.SyntaxTreeCollection"/>
        public abstract IReadOnlyCollection<SyntaxTree> SyntaxTreeCollection { get; }

        /// <inheritdoc cref="IPartialCompilation.TryGetSyntaxTree"/>
        public abstract bool TryGetSyntaxTree( DocumentKey documentKey, [NotNullWhen( true )] out SyntaxTree? syntaxTree );

        /// <summary>
        /// Returns whether the given path is of interest to the current <see cref="PartialCompilation"/>.
        /// This is used to avoid processing of transformations that affect currently irrelevant syntax trees.
        /// </summary>
        internal abstract bool IsSyntaxTreeObserved( string syntaxTreePath );

        /// <summary>
        /// Gets the types declared in the current subset.
        /// </summary>
        public abstract ImmutableHashSet<INamedTypeSymbol> Types { get; }

        /// <summary>
        /// Gets the namespaces that contain types.
        /// </summary>
        public abstract ImmutableHashSet<INamespaceSymbol> Namespaces { get; }

        /// <summary>
        /// Gets a value indicating whether the current <see cref="PartialCompilation"/> is actually partial, or represents a complete compilation.
        /// </summary>
        public abstract bool IsPartial { get; }

        /// <summary>
        /// Gets a value indicating whether <see cref="IsSyntaxTreeObserved"/> may return a different value than <c>true</c>.
        /// </summary>
        internal abstract bool HasObservabilityFilter { get; }

        internal LanguageOptions LanguageOptions
        {
            get
            {
                if ( this.SyntaxTreeCollection.Count > 0 )
                {
                    var parseOptions = (CSharpParseOptions) this.SyntaxTreeCollection.First().Options;

                    return new LanguageOptions( parseOptions );
                }
                else
                {
                    return LanguageOptions.Default;
                }
            }
        }

        // Initial constructor.
        private PartialCompilation( CompilationContext compilationContext, Lazy<DerivedTypeIndex> derivedTypeIndex, ImmutableArray<ManagedResource> resources )
        {
            this.CompilationContext = compilationContext;
            this.InitialCompilation = compilationContext.Compilation;
            this.ModifiedSyntaxTrees = ImmutableDictionary<string, SyntaxTreeTransformation>.Empty;
            this.Resources = resources.IsDefault ? ImmutableArray<ManagedResource>.Empty : resources;
            this.LazyDerivedTypes = derivedTypeIndex;
        }

        // Incremental constructor.
        private PartialCompilation(
            PartialCompilation baseCompilation,
            IReadOnlyCollection<SyntaxTreeTransformation>? modifications,
            ImmutableArray<ManagedResource> newResources )
        {
            this.InitialCompilation = baseCompilation.InitialCompilation;
            var compilation = baseCompilation.Compilation;

            this.LazyDerivedTypes = baseCompilation.LazyDerivedTypes;

            // TODO: accept new relationships to the type index.

            var modifiedTreeBuilder = baseCompilation.ModifiedSyntaxTrees.ToBuilder();

            if ( modifications != null )
            {
                foreach ( var transformation in modifications )
                {
                    if ( transformation.Kind == SyntaxTreeTransformationKind.None )
                    {
                        continue;
                    }

                    // Find the tree in InitialCompilation. When the path has not been modified since, the tree the
                    // transformation applies to is itself the initial tree, so it is taken from the transformation
                    // rather than looked up by path: Compilation.ReplaceSyntaxTree resolves the tree by identity, and
                    // resolving it by path here would let the two disagree.
                    SyntaxTree? initialTree;

                    if ( transformation.OldTree == null )
                    {
                        initialTree = null;
                    }
                    else if ( baseCompilation.ModifiedSyntaxTrees.TryGetValue( transformation.FilePath, out var initialTreeReplacement ) )
                    {
                        initialTree = initialTreeReplacement.OldTree;
                    }
                    else
                    {
                        initialTree = transformation.OldTree;
                    }

                    SyntaxTreeTransformation? transformationFromInitialCompilation;

                    switch ( transformation.Kind )
                    {
                        case SyntaxTreeTransformationKind.Add:
                            compilation = compilation.AddSyntaxTrees( transformation.NewTree! );
                            transformationFromInitialCompilation = transformation;

                            break;

                        case SyntaxTreeTransformationKind.Replace:
                            var newTree = transformation.NewTree.AssertNotNull();
                            compilation = compilation.ReplaceSyntaxTree( transformation.OldTree.AssertNotNull(), newTree );

                            if ( initialTree != null )
                            {
                                transformationFromInitialCompilation = SyntaxTreeTransformation.ReplaceTree( initialTree, newTree );
                            }
                            else
                            {
                                transformationFromInitialCompilation = SyntaxTreeTransformation.AddTree( newTree );
                            }

                            break;

                        case SyntaxTreeTransformationKind.Remove:
                            compilation = compilation.RemoveSyntaxTrees( transformation.OldTree.AssertNotNull() );

                            if ( initialTree != null )
                            {
                                transformationFromInitialCompilation = SyntaxTreeTransformation.RemoveTree( initialTree );
                            }
                            else
                            {
                                transformationFromInitialCompilation = null;
                            }

                            break;

                        default:
                            throw new AssertionFailedException( $"Unexpected transformation kind: {transformation.Kind}." );
                    }

                    if ( transformationFromInitialCompilation != null )
                    {
                        modifiedTreeBuilder[transformation.FilePath] = transformationFromInitialCompilation.Value;
                    }
                    else
                    {
                        modifiedTreeBuilder.Remove( transformation.FilePath );
                    }
                }
            }

            this.ModifiedSyntaxTrees = modifiedTreeBuilder.ToImmutable();
            this.CompilationContext = compilation.GetCompilationContext();
            this.Resources = newResources.IsDefault ? ImmutableArray<ManagedResource>.Empty : newResources;
        }

        /// <summary>
        /// Creates a <see cref="PartialCompilation"/> that represents a complete compilation.
        /// </summary>
        /// <remarks>
        /// The compilation is normalized so that a path identifies a syntax tree of it. See
        /// <see cref="Metalama.Framework.Engine.Utilities.Roslyn.CompilationExtensions.RemoveDuplicatePathSyntaxTrees(Microsoft.CodeAnalysis.Compilation)"/>; the call costs nothing and returns the
        /// argument unchanged when no path is duplicated, which is every compilation produced by the command-line
        /// compiler and every compilation the design-time pipeline has already normalized. It is applied here as well
        /// because <c>AspectDatabase</c>, the preview service and the introspection API reach this method without
        /// passing through the design-time diff layer.
        /// </remarks>
        public static PartialCompilation CreateComplete( Compilation compilation, ImmutableArray<ManagedResource> resources = default )
            => CreateComplete( compilation.RemoveDuplicatePathSyntaxTrees().GetCompilationContext(), resources );

        private static PartialCompilation CreateComplete( CompilationContext compilationContext, ImmutableArray<ManagedResource> resources = default )
            => new CompleteImpl( compilationContext, new Lazy<DerivedTypeIndex>( () => GetDerivedTypeIndex( compilationContext.Compilation ) ), resources );

        /// <summary>
        /// Creates a <see cref="PartialCompilation"/> for a single syntax tree and its closure.
        /// </summary>
        public static PartialCompilation CreatePartial(
            Compilation compilation,
            SyntaxTree syntaxTree,
            ImmutableArray<ManagedResource> resources = default )
        {
            var normalizedCompilation = compilation.RemoveDuplicatePathSyntaxTrees();
            var compilationContext = normalizedCompilation.GetCompilationContext();
            var syntaxTrees = MapToNormalizedCompilation( compilation, normalizedCompilation,new[] { syntaxTree } );
            var closure = GetClosure( compilationContext, syntaxTrees );

            return new PartialImpl(
                compilationContext,
                closure.Trees.ToImmutableDictionary( t => t.FilePath, t => t ),
                observedSyntaxTreePaths: null,
                closure.DeclaredTypes,
                new Lazy<DerivedTypeIndex>( () => closure.DerivedTypeIndex ),
                resources );
        }

        /// <summary>
        /// Creates a <see cref="PartialCompilation"/> for a given subset of syntax trees and its closure.
        /// </summary>
        /// <param name="compilation">The complete compilation.</param>
        /// <param name="syntaxTrees">The trees to include in the partial compilation.</param>
        /// <param name="observedSyntaxTreePaths">List of paths that should return <see langword="true"/> from <see cref="IsSyntaxTreeObserved(string)"/>, or <see langword="null" /> if all paths should be considered observed.</param>
        public static PartialCompilation CreatePartial(
            Compilation compilation,
            IReadOnlyList<SyntaxTree> syntaxTrees,
            ImmutableHashSet<string>? observedSyntaxTreePaths = null,
            ImmutableArray<ManagedResource> resources = default )
        {
            var normalizedCompilation = compilation.RemoveDuplicatePathSyntaxTrees();
            var compilationContext = normalizedCompilation.GetCompilationContext();
            var closure = GetClosure( compilationContext, MapToNormalizedCompilation( compilation, normalizedCompilation,syntaxTrees ) );

            return new PartialImpl(
                compilationContext,
                closure.Trees.ToImmutableDictionary( t => t.FilePath, t => t ),
                observedSyntaxTreePaths,
                closure.DeclaredTypes.ToImmutableHashSet(),
                new Lazy<DerivedTypeIndex>( () => closure.DerivedTypeIndex ),
                resources );
        }

        IPartialCompilation IPartialCompilation.WithSyntaxTreeTransformations( IReadOnlyList<SyntaxTreeTransformation>? transformations )
            => this.Update( transformations );

        public IPartialCompilation WithAdditionalResources( params ManagedResource[] resources ) => this.Update( null, this.Resources.AddRange( resources ) );

        public ImmutableArray<ManagedResource> Resources { get; }

        /// <summary>
        ///  Adds and replaces syntax trees of the current <see cref="PartialCompilation"/> and returns a new <see cref="PartialCompilation"/>
        /// representing the modified object.
        /// </summary>
        public abstract PartialCompilation Update(
            IReadOnlyCollection<SyntaxTreeTransformation>? transformations = null,
            ImmutableArray<ManagedResource> resources = default );

        /// <summary>
        /// Translates the syntax trees the caller asked for, which belong to <paramref name="requestedCompilation"/>,
        /// into the corresponding syntax trees of <paramref name="normalizedCompilation"/>.
        /// </summary>
        /// <param name="requestedCompilation">The compilation the caller passed to <c>CreatePartial</c>.</param>
        /// <param name="normalizedCompilation">
        /// The result of
        /// <see cref="Metalama.Framework.Engine.Utilities.Roslyn.CompilationExtensions.RemoveDuplicatePathSyntaxTrees(Microsoft.CodeAnalysis.Compilation)"/>
        /// on <paramref name="requestedCompilation"/>: the same compilation minus every syntax tree whose path an
        /// earlier tree already held.
        /// </param>
        /// <param name="requestedSyntaxTrees">Syntax trees of <paramref name="requestedCompilation"/>.</param>
        /// <returns>The corresponding syntax trees of <paramref name="normalizedCompilation"/>, without duplicates.</returns>
        /// <remarks>
        /// <para>
        /// The problem this solves: <c>CreatePartial</c> builds the closure against the normalized compilation, and
        /// asking that compilation for the semantic model of a syntax tree it does not contain throws. The caller,
        /// however, selected its trees from the compilation it holds, which is the compilation before normalization. So
        /// whenever normalization removed anything, the two sets have to be reconciled before the closure is computed.
        /// </para>
        /// <para>
        /// Two cases arise, and both are handled by looking the tree up by its path in the normalized compilation.
        /// A tree that survived normalization is found and maps to itself. A tree that was removed, because an earlier
        /// tree held its path, is found as that earlier tree: under the one-document model the two are the same
        /// document, so the caller asking for one is asking for that document, and the surviving tree is what
        /// represents it. Two requested trees can therefore map to one, hence the duplicate check on the way out.
        /// </para>
        /// <para>
        /// A lookup can also fail, which happens only if the caller passes a tree belonging to some other compilation
        /// entirely. Such a tree is dropped rather than reported, matching what <c>CreatePartial</c> did before: it
        /// would previously have produced a closure whose semantic model lookups failed further along.
        /// </para>
        /// <para>
        /// Nothing is allocated and nothing is looked up in the ordinary case, in which no path was duplicated and
        /// normalization returned the compilation itself. See issue #1742.
        /// </para>
        /// </remarks>
        private static IReadOnlyList<SyntaxTree> MapToNormalizedCompilation(
            Compilation requestedCompilation,
            Compilation normalizedCompilation,
            IReadOnlyList<SyntaxTree> requestedSyntaxTrees )
        {
            if ( ReferenceEquals( requestedCompilation, normalizedCompilation ) )
            {
                return requestedSyntaxTrees;
            }

            var syntaxTreesByPath = normalizedCompilation.GetIndexedSyntaxTrees();
            var mappedSyntaxTrees = new List<SyntaxTree>( requestedSyntaxTrees.Count );

            foreach ( var requestedSyntaxTree in requestedSyntaxTrees )
            {
                if ( syntaxTreesByPath.TryGetValue( requestedSyntaxTree.FilePath, out var mappedSyntaxTree )
                     && !mappedSyntaxTrees.Contains( mappedSyntaxTree ) )
                {
                    mappedSyntaxTrees.Add( mappedSyntaxTree );
                }
            }

            return mappedSyntaxTrees;
        }

        private sealed record Closure(
            ImmutableHashSet<INamedTypeSymbol> DeclaredTypes,
            ImmutableHashSet<SyntaxTree> Trees,
            DerivedTypeIndex DerivedTypeIndex );

        /// <summary>
        /// Gets a closure of the syntax trees declaring all base types and interfaces of all types declared in input syntax trees.
        /// </summary>
        private static Closure GetClosure( CompilationContext compilationContext, IReadOnlyList<SyntaxTree> syntaxTrees )
        {
            var assembly = compilationContext.Compilation.Assembly;

            var symbolEqualityComparer = compilationContext.SymbolComparer;

            var types = new HashSet<INamedTypeSymbol>( symbolEqualityComparer );
            var topLevelTypes = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>( symbolEqualityComparer );
            var trees = ImmutableHashSet.CreateBuilder<SyntaxTree>();
            var derivedTypesBuilder = new DerivedTypeIndex.Builder( compilationContext );

            // The trees are those of a compilation from which CompilationExtensions.RemoveDuplicatePathSyntaxTrees has
            // already removed every tree that shared a path with an earlier one, so a path identifies a tree here and
            // membership of the set is enough. The previous guard compared the path of the candidate against the path of
            // every tree already collected, which made building the closure quadratic in the number of trees.
            void AddTree( SyntaxTree newTree ) => trees.Add( newTree );

            void AddTypeRecursive( INamedTypeSymbol type )
            {
                if ( type.Kind == SymbolKind.ErrorType )
                {
                    return;
                }

                var isExternal = !symbolEqualityComparer.Equals( type.ContainingAssembly, assembly );

                if ( isExternal )
                {
                    // If the type is not defined in the current assembly, analyze it using the DerivedTypeIndexBuilder so that
                    // it does not get included in the set of types in the current partial compilation.
                    derivedTypesBuilder.AnalyzeType( type );
                }
                else if ( types.Add( type ) )
                {
                    if ( type.ContainingType == null )
                    {
                        topLevelTypes.Add( type );
                    }

                    // Find relevant syntax trees
                    foreach ( var syntaxReference in type.DeclaringSyntaxReferences )
                    {
                        AddTree( syntaxReference.SyntaxTree );
                    }

                    // Add base types recursively.
                    if ( type.BaseType != null )
                    {
                        var baseType = type.BaseType.OriginalDefinition;
                        derivedTypesBuilder.AddDerivedType( baseType, type );
                        AddTypeRecursive( baseType );
                    }

                    foreach ( var interfaceImpl in type.Interfaces )
                    {
                        var interfaceType = interfaceImpl.OriginalDefinition;
                        derivedTypesBuilder.AddDerivedType( interfaceType, type );
                        AddTypeRecursive( interfaceType );
                    }
                }
                else
                {
                    // The type was already processed.
                }
            }

            var semanticModelProvider = compilationContext.SemanticModelProvider;

            foreach ( var syntaxTree in syntaxTrees )
            {
                // We need to add the SyntaxTree even if it does not contain any type.
                AddTree( syntaxTree );

                var semanticModel = semanticModelProvider.GetSemanticModel( syntaxTree );

                DependencyAnalysisHelper.FindDeclaredTypes(
                    semanticModel,
                    AddTypeRecursive );
            }

            return new Closure( topLevelTypes.ToImmutable(), trees.ToImmutable(), derivedTypesBuilder.ToImmutable() );
        }

        private static DerivedTypeIndex GetDerivedTypeIndex( Compilation compilation )
        {
            var compilationContext = compilation.GetCompilationContext();
            DerivedTypeIndex.Builder builder = new( compilationContext );

            foreach ( var type in compilation.Assembly.GetTypes() )
            {
                builder.AnalyzeType( type );
            }

            return builder.ToImmutable();
        }

        internal ImmutableArray<SyntaxTreeTransformation> ToTransformations() => this.ModifiedSyntaxTrees.Values.ToImmutableArray();

        public override string ToString()
            => $"{{Assembly={this.Compilation.AssemblyName}, SyntaxTrees={this.SyntaxTreeCollection.Count}/{this.Compilation.SyntaxTrees.Count()}}}";

        /// <summary>
        /// Gets the compilation with respect to which the <see cref="ModifiedSyntaxTrees"/> collection has been constructed.
        /// Typically, this is the argument of the <see cref="CreateComplete(Microsoft.CodeAnalysis.Compilation,System.Collections.Immutable.ImmutableArray{Metalama.Compiler.ManagedResource})"/> or <see cref="CreatePartial(Microsoft.CodeAnalysis.Compilation,Microsoft.CodeAnalysis.SyntaxTree,System.Collections.Immutable.ImmutableArray{Metalama.Compiler.ManagedResource})"/>
        /// method, ignoring any modification done by <see cref="Update"/>.
        /// </summary>
        public Compilation InitialCompilation { get; }

        /// <summary>
        /// Gets the <see cref="SyntaxTree"/> that can be used to add new assembly- or module-level attributes.
        /// </summary>
        [Memo]
        internal SyntaxTree SyntaxTreeForCompilationLevelAttributes => this.Compilation.CreateEmptySyntaxTree( "MetalamaAssemblyAttributes.cs" );

        private static void Validate( IReadOnlyCollection<SyntaxTreeTransformation>? transformations )
        {
            // In production scenario, we need weavers to provide SyntaxTree instances with a valid Encoding value.
            // However, we don't need that in test scenarios, and tests currently don't set Encoding properly.
            // The way this test is implemented is to test Encoding in increments only if it is set properly in the initial compilation.
            // It also happens, at design time, that Roslyn does not set the encoding. We also need to be tolerant to this situation.

            if ( transformations != null )
            {
                if ( transformations.Any( t => string.IsNullOrEmpty( t.FilePath ) ) )
                {
                    throw new ArgumentOutOfRangeException( nameof(transformations), "The SyntaxTree.FilePath property must be set to a non-empty value." );
                }

                if ( transformations.Any( t => t.NewTree != null && string.IsNullOrEmpty( t.NewTree.FilePath ) ) )
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(transformations),
                        "The SyntaxTree.FilePath property of the new SyntaxTree must be set to a non-empty value." );
                }

                // We cannot validate the Encoding property because it may be null at design time because of a Roslyn bug, but this does not
                // matter to us in that scenario.
                /*
                 bool HasInitialCompilationEncoding() => this.InitialCompilation.SyntaxTrees.All( t => t.Encoding != null );

                if ( transformations.Any( t => t.NewTree is { Encoding: null } && t.OldTree?.Encoding != null ) && HasInitialCompilationEncoding() )
                {
                    var invalidTrees = transformations.Where( t => t.NewTree is { Encoding: null } ).Select( x => $"'{x.FilePath}'" );

                    throw new ArgumentOutOfRangeException(
                        nameof(transformations),
                        $"The SyntaxTree.Encoding property of these SyntaxTrees cannot be null: {string.Join( ", ", invalidTrees )}" );
                }
                */
            }
        }
    }
}