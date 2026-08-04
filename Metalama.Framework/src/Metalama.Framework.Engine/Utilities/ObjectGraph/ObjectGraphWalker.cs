// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Utilities.ObjectGraph;

/// <summary>
/// Walks a managed object graph breadth-first by reflection, starting from a set of roots supplied by the caller, and
/// reports every object it discovers together with the chain of references that leads to it.
/// </summary>
/// <remarks>
/// <para>
/// The walk is deliberately not a heap walk. It follows only the references reachable from the given roots, which is
/// the set of paths for which the caller is responsible. A reference held through a <see cref="WeakReference"/> or a
/// <see cref="WeakReference{T}"/> is invisible to reflection, because the target is stored in a native handle;
/// therefore weak references are excluded by construction rather than by a rule.
/// </para>
/// <para>
/// A <see cref="ConditionalWeakTable{TKey,TValue}"/> is equally invisible to field reflection, so it is enumerated
/// explicitly, and its entries are followed under ephemeron semantics: the value of an entry is reported only once the
/// key of that entry has been proven reachable by strong references, and a key is never reported as reachable through
/// the table. See <see cref="EnumerateReachableConditionalValues"/> for why this rule is necessary rather than
/// merely accurate.
/// </para>
/// <para>
/// The caller decides where the walk stops, by returning <see cref="ObjectGraphAction.Skip"/> from its visitor for
/// objects whose internal graph is not its concern, and <see cref="ObjectGraphAction.Stop"/> when it has found what
/// it was looking for.
/// </para>
/// <para>
/// The fields of an object are read through an <see cref="ObjectGraphTypeReader"/>, which emits one method per type
/// and is cached for the duration of the walk, so that reflection is used once per type rather than once per field of
/// every instance.
/// </para>
/// </remarks>
internal sealed class ObjectGraphWalker
{
    private readonly ObjectGraphWalkerOptions _options;
    private readonly HashSet<object> _visited = new( ReferenceEqualityComparer<object>.Instance );
    private readonly Dictionary<Type, ObjectGraphTypeReader> _readers = new();

    public ObjectGraphWalker( ObjectGraphWalkerOptions? options = null )
    {
        this._options = options ?? ObjectGraphWalkerOptions.Default;
    }

    /// <summary>
    /// Walks the graph reachable from <paramref name="roots"/> and invokes <paramref name="visitor"/> once for every
    /// distinct object discovered, including the roots themselves.
    /// </summary>
    /// <param name="roots">The objects from which the walk starts, each paired with the name to display for it.</param>
    /// <param name="visitor">
    /// Called once per distinct object, in breadth-first order, and decides whether the walk continues through that
    /// object. Because the order is breadth-first, the <see cref="ObjectGraphNode"/> passed to the visitor carries the
    /// shortest chain of references from a root to the object.
    /// </param>
    public ObjectGraphWalkResult Walk(
        IReadOnlyList<(string Name, object Root)> roots,
        Func<ObjectGraphNode, ObjectGraphAction> visitor )
    {
        this._visited.Clear();

        var stopwatch = Stopwatch.StartNew();
        var queue = new Queue<ObjectGraphNode>();

        foreach ( var (name, root) in roots )
        {
            if ( root == null! )
            {
                continue;
            }

            if ( !this._visited.Add( root ) )
            {
                continue;
            }

            var node = new ObjectGraphNode( root, name, null, false );

            switch ( visitor( node ) )
            {
                case ObjectGraphAction.Stop:
                    return new ObjectGraphWalkResult( this._visited.Count, false, true );

                case ObjectGraphAction.Traverse:
                    queue.Enqueue( node );

                    break;
            }
        }

        // Conditional weak tables are set aside during the strong pass and revisited afterwards, because the value of
        // an entry is only reachable while its key is, and the key may only be proven reachable later.
        var conditionalTables = new List<ObjectGraphNode>();

        while ( true )
        {
            // Strong pass: follow ordinary references until nothing new is discovered.
            while ( queue.Count > 0 )
            {
                if ( this._visited.Count > this._options.MaxObjects || stopwatch.Elapsed > this._options.Timeout )
                {
                    return new ObjectGraphWalkResult( this._visited.Count, true, false );
                }

                var current = queue.Dequeue();

                if ( IsConditionalWeakTable( current.Object.GetType() ) )
                {
                    conditionalTables.Add( current );

                    continue;
                }

                foreach ( var reference in this.EnumerateReferences( current.Object ) )
                {
                    if ( this.Visit( current, reference, queue, visitor ) )
                    {
                        return new ObjectGraphWalkResult( this._visited.Count, false, true );
                    }
                }
            }

            // Conditional pass: follow the value of every entry whose key has been proven reachable by the strong
            // pass. A key that is not reachable cannot keep its value alive, therefore such a value explains nothing.
            foreach ( var table in conditionalTables.ToArray() )
            {
                foreach ( var reference in this.EnumerateReachableConditionalValues( table.Object ) )
                {
                    if ( this.Visit( table, reference, queue, visitor ) )
                    {
                        return new ObjectGraphWalkResult( this._visited.Count, false, true );
                    }
                }
            }

            // The conditional pass may have made new objects reachable, which may in turn make more keys reachable.
            // The walk therefore alternates between the two passes until it reaches a fixed point.
            if ( queue.Count == 0 )
            {
                break;
            }
        }

        return new ObjectGraphWalkResult( this._visited.Count, false, false );
    }

    /// <summary>
    /// Records the discovery of one reference and reports whether the visitor asked to end the walk.
    /// </summary>
    private bool Visit(
        ObjectGraphNode parent,
        in Reference reference,
        Queue<ObjectGraphNode> queue,
        Func<ObjectGraphNode, ObjectGraphAction> visitor )
    {
        if ( !this._visited.Add( reference.Target ) )
        {
            return false;
        }

        var node = new ObjectGraphNode( reference.Target, reference.Label, parent, reference.IsConditional );

        switch ( visitor( node ) )
        {
            case ObjectGraphAction.Stop:
                return true;

            case ObjectGraphAction.Traverse:
                queue.Enqueue( node );

                break;
        }

        return false;
    }

    /// <summary>
    /// Gets the reader of a type, creating and caching it on first use.
    /// </summary>
    /// <remarks>
    /// The cache is held by the walker rather than in a static field on purpose. Its keys are <see cref="Type"/>
    /// objects, some of which belong to a collectible assembly load context, and a static cache would keep every such
    /// context alive for the lifetime of the process, which is exactly the kind of retention this class is used to
    /// find. A walk visits many instances of the same type, so an instance-level cache captures nearly all of the
    /// benefit.
    /// </remarks>
    private ObjectGraphTypeReader GetReader( Type type )
    {
        if ( !this._readers.TryGetValue( type, out var reader ) )
        {
            reader = new ObjectGraphTypeReader( type );
            this._readers.Add( type, reader );
        }

        return reader;
    }

    /// <summary>
    /// Enumerates the outgoing references of an object.
    /// </summary>
    private IEnumerable<Reference> EnumerateReferences( object obj )
    {
        if ( obj is Array array )
        {
            return this.EnumerateArray( array );
        }

        return this.EnumerateFields( obj );
    }

    /// <summary>
    /// Enumerates the elements of a single-dimensional array.
    /// </summary>
    private IEnumerable<Reference> EnumerateArray( Array array )
    {
        if ( array.Rank != 1 )
        {
            yield break;
        }

        var elementType = array.GetType().GetElementType()!;

        if ( elementType.IsPrimitive || elementType.IsEnum || elementType == typeof(decimal) )
        {
            yield break;
        }

        var length = Math.Min( array.Length, this._options.MaxArrayElements );

        if ( !elementType.IsValueType )
        {
            // Array covariance makes an array of any reference type an object[], which reads without the argument
            // checks and the boxing of Array.GetValue.
            var references = (object?[]) array;

            for ( var i = 0; i < length; i++ )
            {
                if ( references[i] is { } value )
                {
                    yield return new Reference( $"[{i}]", value, false );
                }
            }

            yield break;
        }

        var reader = this.GetReader( elementType );

        for ( var i = 0; i < length; i++ )
        {
            object? element;

            try
            {
                element = array.GetValue( i );
            }
            catch ( Exception )
            {
                continue;
            }

            if ( element == null )
            {
                continue;
            }

            var values = reader.Read( element );

            for ( var j = 0; j < values.Length; j++ )
            {
                if ( values[j] is { } value )
                {
                    yield return new Reference( $"[{i}].{reader.Labels[j]}", value, false );
                }
            }
        }
    }

    /// <summary>
    /// Enumerates the instance fields of an object, including the fields declared by base types and those nested inside
    /// value-type fields.
    /// </summary>
    private IEnumerable<Reference> EnumerateFields( object obj )
    {
        var reader = this.GetReader( obj.GetType() );
        var values = reader.Read( obj );

        for ( var i = 0; i < values.Length; i++ )
        {
            if ( values[i] is { } value )
            {
                yield return new Reference( reader.Labels[i], value, false );
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current runtime allows the entries of a
    /// <see cref="ConditionalWeakTable{TKey,TValue}"/> to be enumerated, and therefore whether the walk can follow
    /// conditional references at all.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entries are held through dependent handles, which reflection cannot read, so the only way to reach them is
    /// the <see cref="IEnumerable"/> implementation of the table. .NET Core has one and .NET Framework does not: there
    /// <c>ConditionalWeakTable</c> implements no enumerable interface whatsoever, so nothing held only through such a
    /// table can be discovered.
    /// </para>
    /// <para>
    /// This matters beyond the tests, because <c>Metalama.Framework.Engine</c> targets .NET Framework as well, and that
    /// is the runtime of desktop MSBuild and of Visual Studio. On that runtime the walk is sound but incomplete: every
    /// chain it reports is real, and a retention held only through an ephemeron is invisible to it. The property is
    /// public so that a caller can say so in its report rather than presenting a partial answer as a complete one.
    /// </para>
    /// </remarks>
    public static bool CanFollowConditionalReferences { get; } = (object) new ConditionalWeakTable<object, object>() is IEnumerable;

    /// <summary>
    /// Determines whether a type is a <see cref="ConditionalWeakTable{TKey,TValue}"/>.
    /// </summary>
    private static bool IsConditionalWeakTable( Type type )
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ConditionalWeakTable<,>);

    /// <summary>
    /// Enumerates the values of the entries of a <see cref="ConditionalWeakTable{TKey,TValue}"/> whose key has already
    /// been proven reachable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entries are held through dependent handles, which reflection cannot read, therefore the table is enumerated
    /// through its <see cref="IEnumerable"/> implementation.
    /// </para>
    /// <para>
    /// Keys are never reported as reachable through the table, because a table does not keep its keys alive. An edge
    /// that arrived at an object because that object is a key would name a path along which the garbage collector is
    /// free to reclaim the object, would attribute to the table a retention it does not cause, and would hide the
    /// reference that is genuinely responsible.
    /// </para>
    /// <para>
    /// A value is reported only when its key is already known to be reachable, because that is the condition under
    /// which the entry keeps the value alive. Without this restriction the walk would report that an object is
    /// retained by an entry that the object itself keys, which is circular.
    /// </para>
    /// </remarks>
    private IEnumerable<Reference> EnumerateReachableConditionalValues( object table )
    {
        var references = new List<Reference>();

        if ( !CanFollowConditionalReferences )
        {
            return references;
        }

        try
        {
            var index = 0;

            foreach ( var entry in (IEnumerable) table )
            {
                index++;

                if ( entry == null )
                {
                    continue;
                }

                var entryType = entry.GetType();
                var key = entryType.GetProperty( "Key" )?.GetValue( entry );

                if ( key == null || !this._visited.Contains( key ) )
                {
                    continue;
                }

                var value = entryType.GetProperty( "Value" )?.GetValue( entry );

                if ( value != null )
                {
                    references.Add( new Reference( $"[key #{index}: {ObjectGraphNode.FormatType( key.GetType() )}].Value", value, true ) );
                }
            }
        }
        catch ( Exception )
        {
            // The table cannot be enumerated on this runtime. The absence of these edges only makes the walk less
            // informative, never incorrect.
        }

        return references;
    }

    /// <summary>
    /// An outgoing reference from an object, as discovered by <see cref="EnumerateReferences"/>.
    /// </summary>
    private readonly struct Reference
    {
        public string Label { get; }

        public object Target { get; }

        /// <summary>
        /// Gets a value indicating whether the reference is held through a
        /// <see cref="ConditionalWeakTable{TKey,TValue}"/>, in which case it only extends the lifetime of the target
        /// while the corresponding key is alive.
        /// </summary>
        public bool IsConditional { get; }

        public Reference( string label, object target, bool isConditional )
        {
            this.Label = label;
            this.Target = target;
            this.IsConditional = isConditional;
        }
    }
}
