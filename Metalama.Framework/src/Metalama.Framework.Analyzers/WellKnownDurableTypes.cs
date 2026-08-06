// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The classification of the types whose durability is decided without examining their members.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This table is the static counterpart of <c>UserCodeRetentionPolicy.IsPinning</c> and
    /// <c>UserCodeRetentionPolicy.IsBoundary</c> in <c>Metalama.Framework.Engine</c>, which decide the same question
    /// at run time for the <c>MetalamaDiagnoseMemoryLeaks</c> diagnostic. **The two must be kept in correspondence.**
    /// A user who sees a warning from one and nothing from the other on the same object learns only that one of them
    /// is wrong. This project cannot reference the engine, so the correspondence is enforced by a test rather than by
    /// sharing the list.
    /// </para>
    /// <para>
    /// Two divergences are deliberate, and both follow from this analyzer seeing a declared type where the walker
    /// sees an instance.
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// <c>Diagnostic</c> and <c>Location</c> are not durable here and are absent from <c>IsPinning</c>. The walker
    /// descends into them and reports the syntax tree or the symbol that it actually finds, because whether they pin
    /// anything depends on what they hold. This analyzer cannot descend into an instance it will never see.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Every symbol is not durable here, whereas <c>IsPinning</c> reports only the symbols that belong to the source
    /// of a compilation. A declared type carries no information about the origin of the value that will be stored in
    /// it, so the conservative rule is the only one available. Where a member genuinely holds only metadata symbols,
    /// <c>DurableDangerous{T}</c> states that.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// A verdict established once belongs here rather than in the <c>MetalamaDurableType</c> item of a project, so
    /// that every project benefits from it and the reasoning is recorded in one place.
    /// </para>
    /// </remarks>
    internal static class WellKnownDurableTypes
    {
        private const string _boundaryReason =
            "a chain through this type explains nothing about the object that holds it";

        private static readonly ImmutableArray<int> _valueOnly = ImmutableArray.Create( 1 );

        private static readonly Dictionary<string, WellKnownEntry> _table = CreateTable();

        /// <summary>
        /// Looks up the full metadata name of a generic type definition or of a non-generic type.
        /// </summary>
        public static bool TryGet( string fullMetadataName, out WellKnownEntry entry )
            => _table.TryGetValue( fullMetadataName, out entry );

        private static Dictionary<string, WellKnownEntry> CreateTable()
        {
            var table = new Dictionary<string, WellKnownEntry>( StringComparer.Ordinal );

            void Durable( string name, string? reason = null )
                => table[name] = new WellKnownEntry( WellKnownDurability.Durable, reason );

            void NotDurable( string name, string reason )
                => table[name] = new WellKnownEntry( WellKnownDurability.NotDurable, reason );

            void Transparent( string name, ImmutableArray<int> mask = default )
                => table[name] = new WellKnownEntry( WellKnownDurability.Transparent, null, mask );

            // ---------------------------------------------------------------------------------------------------
            // Always durable: value-like types of the base class library.
            // ---------------------------------------------------------------------------------------------------

            Durable( "System.Guid" );
            Durable( "System.DateTime" );
            Durable( "System.DateTimeOffset" );
            Durable( "System.TimeSpan" );
            Durable( "System.Version" );
            Durable( "System.Uri" );
            Durable( "System.Numerics.BigInteger" );
            Durable( "System.Text.StringBuilder" );

            // A compiled regular expression holds its pattern and its automaton, and reaches nothing else. The naming
            // conventions of Metalama.Patterns.Wpf hold one.
            Durable( "System.Text.RegularExpressions.Regex" );

            // Rooted by the runtime for the lifetime of the process, so retaining one costs nothing. These are the
            // same types at which RetentionPathFinder.ShouldTraverse and UserCodeRetentionPolicy.IsBoundary stop.
            Durable( "System.Type" );
            Durable( "System.Reflection.Assembly" );
            Durable( "System.Reflection.Module" );
            Durable( "System.Reflection.MemberInfo" );
            Durable( "System.Reflection.MethodBase" );
            Durable( "System.Reflection.MethodInfo" );
            Durable( "System.Reflection.FieldInfo" );
            Durable( "System.Reflection.PropertyInfo" );
            Durable( "System.Reflection.ParameterInfo" );
            Durable( "System.Reflection.AssemblyName" );
            Durable( "System.Threading.Thread" );
            Durable( "System.AppDomain" );

            // A cancellation token holds a source, which is scoped to a request and is never bound to a compilation.
            Durable( "System.Threading.CancellationToken" );

            // An exception is admitted so that every custom exception type of the codebase does not become a walk
            // target. This is a judgement call: Exception.Data is a dictionary of object and could in principle carry
            // a compilation-bound value. Nothing in this codebase does that.
            Durable( "System.Exception" );

            // Attribute declares no instance field, so it contributes nothing to a derived type. Without this entry
            // every aspect written as an attribute reports LAMA0873 for a base type that holds nothing, which is the
            // shape of most aspects. The fields the derived attribute declares are still examined.
            Durable( "System.Attribute" );

            // A weak reference is durable whatever it refers to, and its type argument is deliberately not examined.
            // ProjectVersionProvider holds a Dictionary<ProjectKey, WeakReference<Compilation>> and
            // design-time-memory.md presents that as the recommended shape, so recursing into the argument would
            // report the documented good practice as a defect.
            Durable( "System.WeakReference" );
            Durable( "System.WeakReference`1" );

            // ---------------------------------------------------------------------------------------------------
            // Always durable: value-like types of Roslyn.
            // ---------------------------------------------------------------------------------------------------

            Durable( "Microsoft.CodeAnalysis.Text.TextSpan" );
            Durable( "Microsoft.CodeAnalysis.Text.LinePosition" );
            Durable( "Microsoft.CodeAnalysis.Text.LinePositionSpan" );
            Durable( "Microsoft.CodeAnalysis.FileLinePositionSpan" );
            Durable( "Microsoft.CodeAnalysis.SyntaxAnnotation" );
            Durable( "Microsoft.CodeAnalysis.DocumentId" );
            Durable( "Microsoft.CodeAnalysis.ProjectId" );
            Durable( "Microsoft.CodeAnalysis.AssemblyIdentity" );
            Durable( "Microsoft.CodeAnalysis.SyntaxKind" );

            // ---------------------------------------------------------------------------------------------------
            // Always durable: the durable identifiers of this codebase that cannot carry the attribute themselves.
            //
            // SerializableDeclarationId, SerializableTypeId and DocumentKey are deliberately absent: they are marked
            // [Durable] where they are declared, which is better than an entry here because the claim is then
            // verified against their members rather than asserted.
            // ---------------------------------------------------------------------------------------------------

            // Declared in Metalama.Framework.DesignTime.Rpc, which does not reference the contract assembly and is
            // kept dependency-light on purpose, so it cannot carry the attribute.
            Durable( "Metalama.Framework.DesignTime.Rpc.ProjectKey" );

            // The three marker interfaces of the dependency injection scopes. A service is scoped to the pipeline
            // configuration, to the process or to the infrastructure, never to one execution of the pipeline, so
            // holding one is durable by design.
            //
            // These are entries here rather than [Durable] attributes on purpose, and the difference matters. An
            // entry means "trusted, and the members are not examined". The attribute means "verify every member", and
            // applying it to these three brought roughly a hundred services under the contract, most of them holding
            // a semaphore, a task completion source or a cache that is ordinary infrastructure rather than anything
            // bound to a compilation. Durable by lifetime is not the same claim as durable by structure, and only the
            // first is true of a service.
            const string serviceReason = "a service is scoped to the pipeline configuration or wider, never to one pipeline execution";

            Durable( "Metalama.Framework.Services.IProjectService", serviceReason );
            Durable( "Metalama.Framework.Services.IGlobalService", serviceReason );
            Durable( "Metalama.Backstage.Extensibility.IBackstageService", serviceReason );

            // The identity field of a SymbolDictionaryKey is declared as object and holds a string when the key was
            // created by CreatePersistentKey and a symbol when it was created by CreateLookupKey. One type, two
            // lifetimes, so no structural rule can ever accept it, and yet CreatePersistentKey is the documented way
            // to refer to a symbol durably. Splitting the type in two is the real fix.
            Durable( "Metalama.Framework.Engine.Utilities.Roslyn.SymbolDictionaryKey" );

            // A durable reference stores only a serializable identifier. Its type argument is a phantom that exists
            // for the compile-time contract, so it is deliberately not examined: requiring it to be durable would
            // demand that IDeclaration be durable and would reject the most important durable type of the codebase.
            Durable( "Metalama.Framework.Code.IDurableRef" );
            Durable( "Metalama.Framework.Code.IDurableRef`1" );

            // The wrapper by which an author asserts durability that cannot be established.
            Durable( "Metalama.Framework.Utilities.DurableDangerous`1" );

            // ---------------------------------------------------------------------------------------------------
            // Boundaries: project-scoped or process-wide infrastructure, mirroring UserCodeRetentionPolicy.IsBoundary.
            // ---------------------------------------------------------------------------------------------------

            // Both the non-generic base and the generic class. IsBoundary matches on the base, which covers the
            // generic one at run time through inheritance, but the analyzer looks the type up by name and would
            // otherwise miss whichever of the two a member is declared with.
            Durable( "Metalama.Framework.Engine.Services.ServiceProvider", _boundaryReason );
            Durable( "Metalama.Framework.Engine.Services.ServiceProvider`1", _boundaryReason );

            // The wrappers over a service provider. A project service provider is scoped to a pipeline configuration
            // and not to a pipeline execution, so it is durable by design and holding one costs nothing.
            Durable( "Metalama.Framework.Engine.Services.ProjectServiceProvider", _boundaryReason );
            Durable( "Metalama.Framework.Engine.Services.GlobalServiceProvider", _boundaryReason );
            Durable( "Metalama.Framework.Engine.CompileTime.CompileTimeProject", _boundaryReason );
            Durable( "Metalama.Framework.Engine.CompileTime.CompileTimeDomain", _boundaryReason );
            Durable( "Metalama.Framework.Engine.CompileTime.ITemplateReflectionContext", _boundaryReason );
            Durable( "Metalama.Backstage.Diagnostics.ILogger", _boundaryReason );
            Durable( "Metalama.Backstage.Diagnostics.ILoggerFactory", _boundaryReason );

            // ---------------------------------------------------------------------------------------------------
            // Never durable: Roslyn.
            // ---------------------------------------------------------------------------------------------------

            NotDurable(
                "Microsoft.CodeAnalysis.Compilation",
                "a Compilation pins every syntax tree of the project and the symbol tables built from it" );

            NotDurable(
                "Microsoft.CodeAnalysis.CSharp.CSharpCompilation",
                "a Compilation pins every syntax tree of the project and the symbol tables built from it" );

            NotDurable(
                "Microsoft.CodeAnalysis.SyntaxTree",
                "a SyntaxTree pins the full text and the green node tree of a file" );

            NotDurable(
                "Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree",
                "a SyntaxTree pins the full text and the green node tree of a file" );

            NotDurable( "Microsoft.CodeAnalysis.SemanticModel", "a SemanticModel is bound to its compilation" );

            NotDurable(
                "Microsoft.CodeAnalysis.ISymbol",
                "a symbol of the source of a compilation reaches that compilation" );

            NotDurable( "Microsoft.CodeAnalysis.SyntaxNode", "a syntax node reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxToken", "a syntax token reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxTrivia", "a syntax trivia reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxNodeOrToken", "a syntax node reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxList`1", "a syntax node reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SeparatedSyntaxList`1", "a syntax node reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxTokenList", "a syntax token reaches the tree that contains it" );
            NotDurable( "Microsoft.CodeAnalysis.SyntaxTriviaList", "a syntax trivia reaches the tree that contains it" );

            NotDurable( "Microsoft.CodeAnalysis.Location", "a Location holds its source tree" );

            NotDurable(
                "Microsoft.CodeAnalysis.Diagnostic",
                "a Diagnostic holds a Location, which holds its source tree, and holds its message arguments" );

            NotDurable( "Microsoft.CodeAnalysis.IOperation", "an operation is bound to its semantic model" );
            NotDurable( "Microsoft.CodeAnalysis.Text.SourceText", "a SourceText holds the full text of a file" );
            NotDurable( "Microsoft.CodeAnalysis.MetadataReference", "a metadata reference holds a compilation or a metadata block" );
            NotDurable( "Microsoft.CodeAnalysis.Project", "a Project reaches every compilation of the solution" );
            NotDurable( "Microsoft.CodeAnalysis.Solution", "a Solution reaches every compilation of the solution" );
            NotDurable( "Microsoft.CodeAnalysis.Document", "a Document reaches the compilation of its project" );
            NotDurable( "Microsoft.CodeAnalysis.Workspace", "a Workspace reaches every compilation of the solution" );

            // ---------------------------------------------------------------------------------------------------
            // Never durable: the code model.
            // ---------------------------------------------------------------------------------------------------

            NotDurable( "Metalama.Framework.Code.ICompilationElement", "a code model element reaches its CompilationModel" );
            NotDurable( "Metalama.Framework.Code.IRef", "a reference that is not durable holds the symbol and the reference factory" );
            NotDurable( "Metalama.Framework.Code.IRef`1", "a reference that is not durable holds the symbol and the reference factory" );
            NotDurable( "Metalama.Framework.Engine.CodeModel.CompilationModel", "a CompilationModel holds its compilation" );
            NotDurable( "Metalama.Framework.Engine.Services.CompilationContext", "a CompilationContext holds its compilation" );
            NotDurable( "Metalama.Framework.Engine.CodeModel.PartialCompilation", "a PartialCompilation holds its compilation" );
            NotDurable( "Metalama.Framework.Engine.CodeModel.References.RefFactory", "a RefFactory reaches its compilation" );

            // ---------------------------------------------------------------------------------------------------
            // Never durable: types that hold a delegate, and therefore its closure.
            // ---------------------------------------------------------------------------------------------------

            NotDurable(
                "System.Lazy`1",
                "a Lazy holds its factory delegate, and therefore that delegate's closure; use DurableLazy<T>" );

            const string taskReason =
                "a task retains its continuations, and a continuation is the suspended state machine of the awaiting method";

            NotDurable( "System.Threading.Tasks.Task", taskReason );
            NotDurable( "System.Threading.Tasks.Task`1", taskReason );
            NotDurable( "System.Threading.Tasks.ValueTask", taskReason );
            NotDurable( "System.Threading.Tasks.ValueTask`1", taskReason );
            NotDurable( "System.Threading.Tasks.TaskCompletionSource`1", taskReason );

            // ---------------------------------------------------------------------------------------------------
            // Transparent: durable exactly when the type arguments are.
            // ---------------------------------------------------------------------------------------------------

            Transparent( "System.Collections.Generic.IEnumerable`1" );
            Transparent( "System.Collections.Generic.ICollection`1" );
            Transparent( "System.Collections.Generic.IList`1" );
            Transparent( "System.Collections.Generic.IReadOnlyCollection`1" );
            Transparent( "System.Collections.Generic.IReadOnlyList`1" );
            Transparent( "System.Collections.Generic.ISet`1" );
            Transparent( "System.Collections.Generic.IReadOnlySet`1" );
            Transparent( "System.Collections.Generic.List`1" );
            Transparent( "System.Collections.Generic.HashSet`1" );
            Transparent( "System.Collections.Generic.SortedSet`1" );
            Transparent( "System.Collections.Generic.Queue`1" );
            Transparent( "System.Collections.Generic.Stack`1" );
            Transparent( "System.Collections.Generic.LinkedList`1" );
            Transparent( "System.Collections.Generic.LinkedListNode`1" );
            Transparent( "System.Collections.Generic.IDictionary`2" );
            Transparent( "System.Collections.Generic.IReadOnlyDictionary`2" );
            Transparent( "System.Collections.Generic.Dictionary`2" );
            Transparent( "System.Collections.Generic.SortedDictionary`2" );
            Transparent( "System.Collections.Generic.SortedList`2" );
            Transparent( "System.Collections.Generic.KeyValuePair`2" );

            Transparent( "System.Collections.Immutable.ImmutableArray`1" );
            Transparent( "System.Collections.Immutable.ImmutableArray`1+Builder" );
            Transparent( "System.Collections.Immutable.ImmutableList`1" );
            Transparent( "System.Collections.Immutable.ImmutableList`1+Builder" );
            Transparent( "System.Collections.Immutable.IImmutableList`1" );
            Transparent( "System.Collections.Immutable.ImmutableHashSet`1" );
            Transparent( "System.Collections.Immutable.ImmutableHashSet`1+Builder" );
            Transparent( "System.Collections.Immutable.IImmutableSet`1" );
            Transparent( "System.Collections.Immutable.ImmutableSortedSet`1" );
            Transparent( "System.Collections.Immutable.ImmutableStack`1" );
            Transparent( "System.Collections.Immutable.ImmutableQueue`1" );
            Transparent( "System.Collections.Immutable.ImmutableDictionary`2" );
            Transparent( "System.Collections.Immutable.ImmutableDictionary`2+Builder" );
            Transparent( "System.Collections.Immutable.IImmutableDictionary`2" );
            Transparent( "System.Collections.Immutable.ImmutableSortedDictionary`2" );

            Transparent( "System.Collections.Concurrent.ConcurrentDictionary`2" );
            Transparent( "System.Collections.Concurrent.ConcurrentBag`1" );
            Transparent( "System.Collections.Concurrent.ConcurrentQueue`1" );
            Transparent( "System.Collections.Concurrent.ConcurrentStack`1" );

            for ( var arity = 1; arity <= 8; arity++ )
            {
                Transparent( "System.Tuple`" + arity );
                Transparent( "System.ValueTuple`" + arity );
            }

            Transparent( "System.ArraySegment`1" );
            Transparent( "System.Memory`1" );
            Transparent( "System.ReadOnlyMemory`1" );
            Transparent( "System.Runtime.CompilerServices.StrongBox`1" );

            // The collections of this codebase are classified rather than walked, because we vouch for them and
            // because the internals of a skip list are declared as object.
            Transparent( "Metalama.Framework.Engine.Collections.ImmutableDictionaryOfArray`2" );
            Transparent( "Metalama.Framework.Engine.Collections.ImmutableDictionaryOfHashSet`2" );
            Transparent( "Metalama.Framework.Engine.Collections.ImmutableLinkedList`1" );
            Transparent( "Metalama.Framework.Engine.Collections.SkipListDictionary`2" );
            Transparent( "Metalama.Framework.Engine.Collections.DictionaryOfList`2" );
            Transparent( "Metalama.Framework.Engine.Collections.IReadOnlyDictionaryOfList`2" );

            // A conditional weak table does not keep its key alive, so only the value is examined. A value may also
            // reference its own key freely, which is what the cache of ProjectVersionProvider relies on, and which no
            // static rule can distinguish from a reference to a different key.
            Transparent( "System.Runtime.CompilerServices.ConditionalWeakTable`2", _valueOnly );
            Transparent( "Metalama.Framework.Engine.Utilities.Caching.WeakCache`2", _valueOnly );

            return table;
        }
    }
}
