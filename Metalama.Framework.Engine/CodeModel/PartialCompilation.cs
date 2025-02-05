// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel
{
    /// <summary>
    /// Represents a subset of a Roslyn <see cref="Microsoft.CodeAnalysis.Compilation"/>. The subset is limited
    /// to specific syntax trees.
    /// </summary>
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
        public abstract ImmutableDictionary<string, SyntaxTree> SyntaxTrees { get; }

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
                if ( this.SyntaxTrees.Count > 0 )
                {
                    var parseOptions = (CSharpParseOptions) this.SyntaxTrees.Values.First().Options;

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

                    // Find the tree in InitialCompilation.
                    SyntaxTree? initialTree;

                    if ( transformation.OldTree == null )
                    {
                        initialTree = null;
                    }
                    else if ( baseCompilation.ModifiedSyntaxTrees.TryGetValue( transformation.FilePath, out var initialTreeReplacement ) )
                    {
                        initialTree = initialTreeReplacement.OldTree;
                    }
                    else if ( !baseCompilation.SyntaxTrees.TryGetValue( transformation.FilePath, out initialTree! ) )
                    {
                        initialTree = transformation.OldTree.AssertNotNull();
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
        public static PartialCompilation CreateComplete( Compilation compilation, ImmutableArray<ManagedResource> resources = default )
            => CreateComplete( compilation.GetCompilationContext(), resources );

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
            var compilationContext = compilation.GetCompilationContext();
            var syntaxTrees = new[] { syntaxTree };
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
            var compilationContext = compilation.GetCompilationContext();
            var closure = GetClosure( compilationContext, syntaxTrees );

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

            void AddTree( SyntaxTree newTree )
            {
                // At design time, the collection of syntax trees can contain duplicates.
                if ( trees.All( t => t.FilePath != newTree.FilePath ) )
                {
                    trees.Add( newTree );
                }
            }

            void AddTypeRecursive( INamedTypeSymbol type )
            {
                if ( type is IErrorTypeSymbol )
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
            => $"{{Assembly={this.Compilation.AssemblyName}, SyntaxTrees={this.SyntaxTrees.Count}/{this.Compilation.SyntaxTrees.Count()}}}";

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