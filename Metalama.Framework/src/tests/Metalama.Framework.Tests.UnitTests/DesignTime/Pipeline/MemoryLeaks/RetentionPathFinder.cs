// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline.MemoryLeaks;

/// <summary>
/// Finds the shortest chain of object references from a set of roots to a target object, by walking the object graph
/// with reflection.
/// </summary>
/// <remarks>
/// <para>
/// A test that asserts that a <see cref="Compilation"/> has been collected reports a failure that is difficult to act
/// upon: the assertion says that something retains the compilation, but not what. This class answers that question.
/// The roots given to <see cref="TryFindPath"/> are the objects that a design-time host keeps alive for the lifetime
/// of a project, typically the pipeline factory and the services it owns, and the target is the object that the test
/// expected to be collected.
/// </para>
/// <para>
/// The walk is deliberately not a heap walk. It only follows fields that are reachable from the given roots, which is
/// exactly the set of paths that Metalama is responsible for. A reference held through a
/// <see cref="WeakReference"/> or a <see cref="WeakReference{T}"/> is invisible to reflection, because the target is
/// stored in a native handle, therefore weak references are naturally excluded. A
/// <see cref="ConditionalWeakTable{TKey,TValue}"/> is equally invisible, so it is enumerated explicitly and its edges
/// are reported as conditional, because such an edge only keeps the value alive while the key is alive.
/// </para>
/// <para>
/// The walk stops at objects that are themselves candidate targets, such as a <see cref="Compilation"/> or a
/// <see cref="SyntaxTree"/>, because the internal graph of those objects is large and is not the responsibility of
/// this codebase.
/// </para>
/// </remarks>
internal sealed class RetentionPathFinder
{
    /// <summary>
    /// The maximum number of distinct objects visited before the search gives up.
    /// </summary>
    private const int _defaultMaxObjects = 400_000;

    /// <summary>
    /// The maximum number of elements inspected in a single array.
    /// </summary>
    private const int _maxArrayElements = 100_000;

    /// <summary>
    /// The maximum nesting depth when recursing into the fields of a value type.
    /// </summary>
    private const int _maxStructDepth = 6;

    private readonly int _maxObjects;
    private readonly TimeSpan _timeout;
    private readonly Dictionary<object, Edge> _parents = new( ReferenceEqualityComparer.Instance );
    private readonly HashSet<object> _visited = new( ReferenceEqualityComparer.Instance );

    /// <summary>
    /// Initializes a new instance of the <see cref="RetentionPathFinder"/> class.
    /// </summary>
    /// <param name="maxObjects">The maximum number of distinct objects to visit. The search reports failure when the budget is exhausted.</param>
    /// <param name="timeout">The maximum duration of the search. A <c>null</c> value means one minute.</param>
    public RetentionPathFinder( int maxObjects = _defaultMaxObjects, TimeSpan? timeout = null )
    {
        this._maxObjects = maxObjects;
        this._timeout = timeout ?? TimeSpan.FromMinutes( 1 );
    }

    /// <summary>
    /// Gets the number of distinct objects visited by the last call to <see cref="TryFindPath"/>.
    /// </summary>
    public int VisitedObjectCount => this._visited.Count;

    /// <summary>
    /// Gets a value indicating whether the last call to <see cref="TryFindPath"/> stopped because it exhausted its
    /// budget of objects or its timeout, in which case a negative result is inconclusive.
    /// </summary>
    public bool IsExhausted { get; private set; }

    /// <summary>
    /// Attempts to find the shortest chain of strong or conditional references from one of <paramref name="roots"/>
    /// to <paramref name="target"/>.
    /// </summary>
    /// <param name="target">The object whose retention must be explained.</param>
    /// <param name="path">On success, a human-readable description of the chain, one hop per line.</param>
    /// <param name="roots">The objects from which the search starts, each paired with the name to display for it.</param>
    /// <returns><c>true</c> if a chain was found.</returns>
    public bool TryFindPath( object target, out string? path, params (string Name, object Root)[] roots )
    {
        this._parents.Clear();
        this._visited.Clear();
        this.IsExhausted = false;

        var stopwatch = Stopwatch.StartNew();
        var queue = new Queue<object>();

        foreach ( var (name, root) in roots )
        {
            if ( root == null! )
            {
                continue;
            }

            if ( ReferenceEquals( root, target ) )
            {
                path = $"{name} ({FormatType( root.GetType() )}) is the target itself.";

                return true;
            }

            if ( this._visited.Add( root ) )
            {
                this._parents[root] = new Edge( null, name, false );
                queue.Enqueue( root );
            }
        }

        // Conditional weak tables are set aside during the strong pass and revisited afterwards, because the value of
        // an entry is only reachable while its key is, and the key may only be proven reachable later.
        List<object> conditionalTables = new();

        while ( true )
        {
            // Strong pass: follow ordinary references until nothing new is discovered.
            while ( queue.Count > 0 )
            {
                if ( this._visited.Count > this._maxObjects || stopwatch.Elapsed > this._timeout )
                {
                    this.IsExhausted = true;
                    path = null;

                    return false;
                }

                var current = queue.Dequeue();

                if ( IsConditionalWeakTable( current.GetType() ) )
                {
                    conditionalTables.Add( current );

                    continue;
                }

                foreach ( var reference in EnumerateReferences( current ) )
                {
                    if ( this.Visit( current, reference, target, queue, out path ) )
                    {
                        return true;
                    }
                }
            }

            // Conditional pass: follow the value of every entry whose key has been proven reachable by the strong
            // pass. A key that is not reachable cannot keep its value alive, therefore such a value explains nothing.
            // This is the marking rule the garbage collector itself applies to ephemerons, and applying it here is
            // what prevents the search from reporting that an object is retained by a table entry that the object
            // itself keys.
            foreach ( var table in conditionalTables.ToArray() )
            {
                foreach ( var reference in this.EnumerateReachableConditionalValues( table ) )
                {
                    if ( this.Visit( table, reference, target, queue, out path ) )
                    {
                        return true;
                    }
                }
            }

            // The conditional pass may have made new objects reachable, which may in turn make more keys reachable.
            // The search therefore alternates between the two passes until it reaches a fixed point.
            if ( queue.Count == 0 )
            {
                break;
            }
        }

        path = null;

        return false;
    }

    /// <summary>
    /// Records the discovery of one reference, and reports whether it reaches the target.
    /// </summary>
    private bool Visit( object parent, Reference reference, object target, Queue<object> queue, out string? path )
    {
        var child = reference.Target;

        if ( ReferenceEquals( child, target ) )
        {
            this._parents[child] = new Edge( parent, reference.Label, reference.IsConditional );
            path = this.FormatPath( child );

            return true;
        }

        path = null;

        if ( !this._visited.Add( child ) )
        {
            return false;
        }

        this._parents[child] = new Edge( parent, reference.Label, reference.IsConditional );

        if ( ShouldTraverse( child ) )
        {
            queue.Enqueue( child );
        }

        return false;
    }

    /// <summary>
    /// Builds the human-readable description of the chain that leads to <paramref name="target"/>.
    /// </summary>
    private string FormatPath( object target )
    {
        var hops = new List<Edge>();
        var hopTargets = new List<object>();
        var current = target;

        while ( this._parents.TryGetValue( current, out var edge ) )
        {
            hops.Add( edge );
            hopTargets.Add( current );

            if ( edge.Parent == null )
            {
                break;
            }

            current = edge.Parent;
        }

        hops.Reverse();
        hopTargets.Reverse();

        var stringBuilder = new StringBuilder();

        for ( var i = 0; i < hops.Count; i++ )
        {
            var indent = new string( ' ', i * 2 );
            var kind = hops[i].IsConditional ? " [conditional]" : "";
            stringBuilder.AppendLine( $"{indent}{hops[i].Label} : {FormatType( hopTargets[i].GetType() )}{kind}" );
        }

        return stringBuilder.ToString().TrimEnd();
    }

    /// <summary>
    /// Determines whether the object graph should be explored through the given object.
    /// </summary>
    /// <remarks>
    /// Objects that are themselves plausible targets of a retention assertion, and objects belonging to the reflection
    /// or threading infrastructure, are not traversed. Traversing them would multiply the cost of the search without
    /// producing a chain that this codebase could act upon.
    /// </remarks>
    private static bool ShouldTraverse( object obj )
    {
        switch ( obj )
        {
            case Compilation:
            case SyntaxTree:
            case SemanticModel:
            case ISymbol:
            case string:
            case Type:
            case Assembly:
            case Module:
            case MemberInfo:
            case ParameterInfo:
            case Thread:
            case AppDomain:
                return false;

            default:
                return true;
        }
    }

    /// <summary>
    /// Enumerates the outgoing strong and conditional references of an object.
    /// </summary>
    private static IEnumerable<Reference> EnumerateReferences( object obj )
    {
        var type = obj.GetType();

        if ( obj is Array array )
        {
            foreach ( var reference in EnumerateArray( array ) )
            {
                yield return reference;
            }

            yield break;
        }

        foreach ( var reference in EnumerateFields( obj, type, "", 0 ) )
        {
            yield return reference;
        }
    }

    /// <summary>
    /// Enumerates the elements of a single-dimensional array.
    /// </summary>
    private static IEnumerable<Reference> EnumerateArray( Array array )
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

        var length = Math.Min( array.Length, _maxArrayElements );

        for ( var i = 0; i < length; i++ )
        {
            object? value;

            try
            {
                value = array.GetValue( i );
            }
            catch ( Exception )
            {
                continue;
            }

            if ( value == null )
            {
                continue;
            }

            if ( elementType.IsValueType )
            {
                foreach ( var reference in EnumerateFields( value, elementType, $"[{i}]", 1 ) )
                {
                    yield return reference;
                }
            }
            else
            {
                yield return new Reference( $"[{i}]", value, false );
            }
        }
    }

    /// <summary>
    /// Enumerates the instance fields of an object or of a boxed value, including the fields declared by base types.
    /// </summary>
    /// <param name="obj">The object or boxed value whose fields are read.</param>
    /// <param name="type">The type whose fields are enumerated. For a boxed value this is the declared type of the value.</param>
    /// <param name="prefix">The label prefix, used when recursing into the fields of a value type.</param>
    /// <param name="structDepth">The current nesting depth inside value types.</param>
    private static IEnumerable<Reference> EnumerateFields( object obj, Type type, string prefix, int structDepth )
    {
        for ( var currentType = type; currentType != null && currentType != typeof(object); currentType = currentType.BaseType )
        {
            FieldInfo[] fields;

            try
            {
                fields = currentType.GetFields( BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly );
            }
            catch ( Exception )
            {
                yield break;
            }

            foreach ( var field in fields )
            {
                var fieldType = field.FieldType;

                if ( fieldType.IsPrimitive || fieldType.IsEnum || fieldType.IsPointer || fieldType == typeof(IntPtr) || fieldType == typeof(UIntPtr) )
                {
                    continue;
                }

                object? value;

                try
                {
                    value = field.GetValue( obj );
                }
                catch ( Exception )
                {
                    // Fields of by-reference-like types cannot be read by reflection, and some runtime types refuse
                    // access. Such a field cannot be the cause of a managed retention that this test could fix.
                    continue;
                }

                if ( value == null )
                {
                    continue;
                }

                var label = prefix.Length == 0 ? field.Name : $"{prefix}.{field.Name}";

                if ( fieldType.IsValueType )
                {
                    if ( structDepth >= _maxStructDepth )
                    {
                        continue;
                    }

                    foreach ( var reference in EnumerateFields( value, fieldType, label, structDepth + 1 ) )
                    {
                        yield return reference;
                    }
                }
                else
                {
                    yield return new Reference( label, value, false );
                }
            }
        }
    }

    /// <summary>
    /// Determines whether a type is a <see cref="ConditionalWeakTable{TKey,TValue}"/>.
    /// </summary>
    private static bool IsConditionalWeakTable( Type type )
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ConditionalWeakTable<,>);

    /// <summary>
    /// Enumerates the values of the entries of a <see cref="ConditionalWeakTable{TKey,TValue}"/> whose key has
    /// already been proven reachable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entries are held through dependent handles, which reflection cannot read, therefore the table is
    /// enumerated through its <see cref="IEnumerable"/> implementation.
    /// </para>
    /// <para>
    /// Keys are never reported as reachable through the table, because a table does not keep its keys alive. An edge
    /// that arrived at an object because that object is a key would name a path along which the garbage collector is
    /// free to reclaim the object, would attribute to the table a retention it does not cause, and would hide the
    /// reference that is genuinely responsible.
    /// </para>
    /// <para>
    /// A value is reported only when its key is already known to be reachable, because that is the condition under
    /// which the entry keeps the value alive. Without this restriction the search would report that an object is
    /// retained by an entry that the object itself keys, which is circular.
    /// </para>
    /// </remarks>
    private IEnumerable<Reference> EnumerateReachableConditionalValues( object table )
    {
        List<Reference> references = new();

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
                    references.Add( new Reference( $"[key #{index}: {FormatType( key.GetType() )}].Value", value, true ) );
                }
            }
        }
        catch ( Exception )
        {
            // The table cannot be enumerated on this runtime. The absence of these edges only makes the search less
            // informative, never incorrect.
        }

        return references;
    }

    /// <summary>
    /// Formats a type name in a form that is short enough to read in a test failure message.
    /// </summary>
    private static string FormatType( Type type )
    {
        if ( !type.IsGenericType )
        {
            return type.Name;
        }

        var name = type.Name;
        var backTick = name.IndexOf( '`' );

        if ( backTick > 0 )
        {
            name = name.Substring( 0, backTick );
        }

        var arguments = type.GetGenericArguments();
        var formattedArguments = new string[arguments.Length];

        for ( var i = 0; i < arguments.Length; i++ )
        {
            formattedArguments[i] = FormatType( arguments[i] );
        }

        return $"{name}<{string.Join( ",", formattedArguments )}>";
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

    /// <summary>
    /// The incoming edge of a visited object, used to reconstruct the chain once the target has been found.
    /// </summary>
    private readonly struct Edge
    {
        public object? Parent { get; }

        public string Label { get; }

        public bool IsConditional { get; }

        public Edge( object? parent, string label, bool isConditional )
        {
            this.Parent = parent;
            this.Label = label;
            this.IsConditional = isConditional;
        }
    }

    /// <summary>
    /// Compares objects by reference, so that the search is not affected by user-defined equality.
    /// </summary>
    /// <remarks>
    /// The framework type of the same name is not available on all target frameworks of this test project, therefore
    /// it is defined here.
    /// </remarks>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        private ReferenceEqualityComparer() { }

        public new bool Equals( object? x, object? y ) => ReferenceEquals( x, y );

        public int GetHashCode( object obj ) => RuntimeHelpers.GetHashCode( obj );
    }
}
