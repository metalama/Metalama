// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Analyzers
{
    /// <summary>
    /// The classification of the types whose immutability is decided without examining their members.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This table is the static counterpart of <c>ImmutabilityExtensions.GetImmutabilityKind</c> and of the collection
    /// list in <c>Fabric</c>, both in <c>Metalama.Patterns.Immutability</c>, which decide the same question over the
    /// Metalama code model. **The two must be kept in correspondence.** This project cannot reference the patterns
    /// assembly, nor the engine that its options system needs, so the correspondence is enforced by a test rather than
    /// by sharing the list.
    /// </para>
    /// <para>
    /// Four divergences from the patterns implementation are deliberate.
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// <c>Nullable{T}</c> is inspected here. The patterns rule reaches it through the blanket rule for value types of
    /// namespace <c>System</c> and therefore trusts it whatever <c>T</c> is, which is unsound for a nullable mutable
    /// struct.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// A tuple is transparent here. A faithful port classifies <c>(string, int)</c> as mutable, because
    /// <c>ValueTuple</c> is excluded from the blanket rule and is not a <c>readonly struct</c>. That is correct in
    /// the sense that a tuple field can be reassigned, but as the type of a <c>readonly</c> field it cannot be, and
    /// reporting it would be a false positive on almost every use.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>System.ArraySegment{T}</c> is mutable here. It is a <c>readonly struct</c> of namespace <c>System</c>, so
    /// the blanket rule would call it deeply immutable, but it wraps an array whose elements can be replaced.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Nothing. <c>ImmutableSortedDictionary{TKey,TValue}</c> and <c>ImmutableQueue{T}</c> were missing from the
    /// patterns list, which was an oversight rather than a decision, the more clearly so because
    /// <c>IImmutableQueue{T}</c> was registered while the concrete type was not. They were added to
    /// <c>Metalama.Patterns.Immutability.Fabric</c> at the same time as this table was written, so the two lists are
    /// equal and <c>ImmutableTableCorrespondenceTests</c> asserts exactly that.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>This table disagrees with <c>WellKnownDurableTypes</c> on purpose, and the disagreement is not a defect.</b>
    /// A delegate is immutable, because it cannot be retargeted, and is not durable, because it holds its target and
    /// its closure. An array is durable when its elements are, and is never immutable. A
    /// <c>WeakReference{T}</c> is durable whatever <c>T</c> is, and is mutable because it can be retargeted. The two
    /// contracts answer different questions, and <c>DurabilityImmutabilityDivergenceTests</c> asserts each of these
    /// so that a future reader cannot mistake one for a bug in the other.
    /// </para>
    /// <para>
    /// A verdict established once belongs here rather than in the <c>MetalamaImmutableType</c> item of a project, so
    /// that every project benefits from it and the reasoning is recorded in one place.
    /// </para>
    /// </remarks>
    internal static class WellKnownImmutableTypes
    {
        private const string _immutableCollectionNamespace = "System.Collections.Immutable.";

        /// <summary>
        /// The immutable collections, by metadata name without the namespace.
        /// </summary>
        /// <remarks>
        /// Held apart from the table so that the correspondence test can compare this list, and only this list, with
        /// the one that <c>Metalama.Patterns.Immutability.Fabric</c> registers.
        /// </remarks>
        public static readonly ImmutableArray<string> ImmutableCollectionNames =
            ImmutableArray.Create(
                "IImmutableDictionary`2",
                "IImmutableList`1",
                "IImmutableQueue`1",
                "IImmutableSet`1",
                "IImmutableStack`1",
                "ImmutableArray`1",
                "ImmutableDictionary`2",
                "ImmutableHashSet`1",
                "ImmutableList`1",
                "ImmutableQueue`1",
                "ImmutableSortedDictionary`2",
                "ImmutableSortedSet`1",
                "ImmutableStack`1" );

        /// <summary>
        /// The names of the value types of namespace <c>System</c> that the blanket rule must not trust, copied from
        /// <c>ImmutabilityExtensions.IsNonImmutableSystemValueType</c>.
        /// </summary>
        public static readonly ImmutableArray<string> NonImmutableSystemValueTypeNames =
            ImmutableArray.Create( "ValueTuple", "Span", "ReadOnlySpan", "Memory", "ReadOnlyMemory" );

        private static readonly Dictionary<string, WellKnownImmutabilityEntry> _table = CreateTable();

        /// <summary>
        /// Looks up the full metadata name of a generic type definition or of a non-generic type.
        /// </summary>
        public static bool TryGet( string fullMetadataName, out WellKnownImmutabilityEntry entry )
            => _table.TryGetValue( fullMetadataName, out entry );

        /// <summary>
        /// Gets every name the table classifies, so that a test can assert that each one resolves to a real type.
        /// </summary>
        public static IEnumerable<string> AllNames => _table.Keys;

        private static Dictionary<string, WellKnownImmutabilityEntry> CreateTable()
        {
            var table = new Dictionary<string, WellKnownImmutabilityEntry>( StringComparer.Ordinal );

            void Immutable( string name, string? reason = null )
                => table[name] = new WellKnownImmutabilityEntry( WellKnownImmutability.Immutable, reason );

            void NotImmutable( string name, string reason )
                => table[name] = new WellKnownImmutabilityEntry( WellKnownImmutability.NotImmutable, reason );

            void Transparent( string name, ImmutableArray<int> mask = default )
                => table[name] = new WellKnownImmutabilityEntry( WellKnownImmutability.Transparent, null, mask );

            // ----------------------------------------------------------------------------------------------------
            // The immutable collections. Transparent, which is what makes the contract deep: an
            // ImmutableArray<StringBuilder> holds references that can be used to mutate, and is reported.
            // ----------------------------------------------------------------------------------------------------

            foreach ( var name in ImmutableCollectionNames )
            {
                Transparent( _immutableCollectionNamespace + name );
            }

            // The builders are deliberately absent from the list above and are named here instead. They are mutable by
            // construction, which is their entire purpose. Note that WellKnownDurableTypes does classify them, as
            // durable, because durability and immutability are different questions.
            NotImmutable( "System.Collections.Immutable.ImmutableArray`1+Builder", "a builder is mutable by construction" );
            NotImmutable( "System.Collections.Immutable.ImmutableDictionary`2+Builder", "a builder is mutable by construction" );
            NotImmutable( "System.Collections.Immutable.ImmutableHashSet`1+Builder", "a builder is mutable by construction" );
            NotImmutable( "System.Collections.Immutable.ImmutableList`1+Builder", "a builder is mutable by construction" );
            NotImmutable( "System.Collections.Immutable.ImmutableSortedDictionary`2+Builder", "a builder is mutable by construction" );
            NotImmutable( "System.Collections.Immutable.ImmutableSortedSet`1+Builder", "a builder is mutable by construction" );

            // ----------------------------------------------------------------------------------------------------
            // Reference types of the base class library that are effectively immutable.
            //
            // These need an entry because the default rule classifies every reference type as mutable, and each of
            // them appears in a real aspect: InvalidateCacheAttribute holds a Type, and the WPF naming conventions
            // hold a Regex.
            // ----------------------------------------------------------------------------------------------------

            Immutable( "System.Type" );
            Immutable( "System.Reflection.Assembly" );
            Immutable( "System.Reflection.Module" );
            Immutable( "System.Reflection.MemberInfo" );
            Immutable( "System.Reflection.MethodBase" );
            Immutable( "System.Reflection.MethodInfo" );
            Immutable( "System.Reflection.ConstructorInfo" );
            Immutable( "System.Reflection.FieldInfo" );
            Immutable( "System.Reflection.PropertyInfo" );
            Immutable( "System.Reflection.EventInfo" );
            Immutable( "System.Reflection.ParameterInfo" );
            Immutable( "System.Uri" );
            Immutable( "System.Version" );
            Immutable( "System.Text.RegularExpressions.Regex" );

            // Without this entry, every aspect written as an attribute reports its own base type. System.Attribute
            // declares no instance field, so there is nothing there to be mutable. WellKnownDurableTypes carries the
            // same entry for the same reason.
            Immutable( "System.Attribute", "Attribute declares no instance field" );

            // ----------------------------------------------------------------------------------------------------
            // Metalama types that an aspect routinely stores.
            //
            // SerializableDeclarationId, SerializableTypeId and DocumentKey are deliberately absent: they live in the
            // contract assembly and should carry [ImmutableObject(true)] at their declaration, where the analyzer
            // verifies the claim instead of taking it on trust.
            // ----------------------------------------------------------------------------------------------------

            // Logically immutable: every mutator returns a new instance through Create, and the type is already
            // [Durable]. It cannot be written in an immutable style, and the reason is structural rather than
            // accidental. Its serializer derives from ReferenceTypeSerializer<T>, which splits construction from
            // field assignment -- CreateInstance then DeserializeFields -- so that a cycle in an object graph can be
            // broken. A field that DeserializeFields restores therefore cannot be readonly. The second writeable
            // field is a memoized Count, marked [NonCompileTimeSerialized] for the same reason it exists.
            //
            // This is the general case, not one type: no ICompileTimeSerializable type with a hand-written
            // ReferenceTypeSerializer can satisfy the read-only rule, and IAspect derives from
            // ICompileTimeSerializable. See immutability-FINDINGS-TODO.md.
            Transparent( "Metalama.Framework.Options.IncrementalKeyedCollection`2" );

            Immutable( "Metalama.Framework.Code.IRef" );
            Immutable( "Metalama.Framework.Code.IRef`1" );
            Immutable( "Metalama.Framework.Code.IDurableRef" );
            Immutable( "Metalama.Framework.Code.IDurableRef`1" );

            // ----------------------------------------------------------------------------------------------------
            // Types that are named as mutable rather than left to fall through, so that the message explains itself.
            // ----------------------------------------------------------------------------------------------------

            NotImmutable( "System.Text.StringBuilder", "a StringBuilder is mutable" );

            // Durable, and mutable. See the remark on the disagreement above.
            NotImmutable( "System.WeakReference", "a weak reference can be retargeted" );
            NotImmutable( "System.WeakReference`1", "a weak reference can be retargeted" );

            // A readonly struct of namespace System that wraps a mutable array, so the blanket rule must not reach it.
            NotImmutable( "System.ArraySegment`1", "an ArraySegment wraps a mutable array" );

            const string mutableCollection = "a mutable collection; use the corresponding type of System.Collections.Immutable";

            NotImmutable( "System.Collections.Generic.List`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.Dictionary`2", mutableCollection );
            NotImmutable( "System.Collections.Generic.HashSet`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.Queue`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.Stack`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.LinkedList`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.SortedSet`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.SortedList`2", mutableCollection );
            NotImmutable( "System.Collections.Generic.SortedDictionary`2", mutableCollection );

            // The mutating interfaces. Their read-only counterparts are deliberately absent: IEnumerable<T>,
            // IReadOnlyList<T>, IReadOnlyCollection<T> and IReadOnlyDictionary<TKey,TValue> are interfaces, so the
            // rule for an unannotated interface reports them with the message that the runtime value may still be a
            // List<T>. That is correct, and it is the reason ImmutableArray<T> exists.
            NotImmutable( "System.Collections.Generic.ICollection`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.IList`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.ISet`1", mutableCollection );
            NotImmutable( "System.Collections.Generic.IDictionary`2", mutableCollection );

            NotImmutable( "System.Collections.Concurrent.ConcurrentDictionary`2", mutableCollection );
            NotImmutable( "System.Collections.Concurrent.ConcurrentBag`1", mutableCollection );
            NotImmutable( "System.Collections.Concurrent.ConcurrentQueue`1", mutableCollection );
            NotImmutable( "System.Collections.Concurrent.ConcurrentStack`1", mutableCollection );

            // The Roslyn roots. Naming them is not strictly necessary, since they would fall through to the default
            // rule, but it makes the message useful, and the rule that walks base types and interfaces then covers
            // every symbol, syntax and operation type without listing them.
            NotImmutable( "Microsoft.CodeAnalysis.Compilation", "a compilation is rebuilt on every edit" );
            NotImmutable( "Microsoft.CodeAnalysis.SemanticModel", "a semantic model belongs to one compilation" );
            NotImmutable( "Microsoft.CodeAnalysis.SyntaxNode", "a syntax node belongs to one syntax tree" );
            NotImmutable( "Microsoft.CodeAnalysis.Location", "a location may hold a syntax tree" );

            return table;
        }
    }
}
