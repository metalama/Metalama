// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.ObjectGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests <see cref="ObjectGraphWalker"/> on object graphs built for the purpose.
/// </summary>
/// <remarks>
/// The walker is shared by the fabric retention diagnostic and by the memory-leak test harness, and its rules are of
/// the kind that a defect leaves silent: a missed field makes a retention invisible, and a conditional-weak-table entry
/// followed too eagerly invents one. Each rule therefore has a test of its own here, on a graph small enough that the
/// expected answer is evident by inspection.
/// </remarks>
public sealed class ObjectGraphWalkerTests
{
    /// <summary>
    /// Walks a graph from a single root and returns every node discovered, indexed by the object.
    /// </summary>
    private static IReadOnlyList<ObjectGraphNode> Walk(
        object root,
        ObjectGraphWalkerOptions? options = null,
        Func<ObjectGraphNode, ObjectGraphAction>? visitor = null )
        => WalkMany( [("root", root)], options, visitor );

    private static IReadOnlyList<ObjectGraphNode> WalkMany(
        IReadOnlyList<(string Name, object Root)> roots,
        ObjectGraphWalkerOptions? options = null,
        Func<ObjectGraphNode, ObjectGraphAction>? visitor = null )
    {
        var nodes = new List<ObjectGraphNode>();

        new ObjectGraphWalker( options ).Walk(
            roots,
            node =>
            {
                nodes.Add( node );

                return visitor?.Invoke( node ) ?? ObjectGraphAction.Traverse;
            } );

        return nodes;
    }

    private static ObjectGraphNode? Find( IReadOnlyList<ObjectGraphNode> nodes, object target )
        => nodes.FirstOrDefault( n => ReferenceEquals( n.Object, target ) );

    private static bool Reached( IReadOnlyList<ObjectGraphNode> nodes, object target ) => Find( nodes, target ) != null;

// The fields below are written and never read by this code on purpose: what reads them is the walker, by reflection,
// which is the whole point of the test.
#pragma warning disable SA1401, CS0649, IDE0044, IDE0051, IDE0052, RS0030

    private sealed class Node
    {
        public string Name;
        public Node? Next;
        public Node? Other;

        public Node( string name )
        {
            this.Name = name;
        }
    }

    [Fact]
    public void Cycle_Terminates()
    {
        var a = new Node( "a" );
        var b = new Node( "b" );
        a.Next = b;
        b.Next = a;

        var nodes = Walk( a );

        Assert.True( Reached( nodes, b ) );
        Assert.Equal( 1, nodes.Count( n => ReferenceEquals( n.Object, b ) ) );
    }

    [Fact]
    public void SelfReference_Terminates()
    {
        var a = new Node( "a" );
        a.Next = a;

        var nodes = Walk( a );

        Assert.Single( nodes, n => ReferenceEquals( n.Object, a ) );
    }

    [Fact]
    public void Diamond_ReportsTheShortestPath()
    {
        // The target is two hops away through Other and three hops away through Next. Because the walk is
        // breadth-first, the recorded path must be the short one.
        var root = new Node( "root" );
        var middle = new Node( "middle" );
        var far = new Node( "far" );
        var target = new Node( "target" );

        root.Other = middle;
        middle.Next = target;

        root.Next = far;
        far.Next = middle;

        var node = Find( Walk( root ), target );

        Assert.NotNull( node );
        Assert.Equal( 2, node!.Depth );
        Assert.Equal( "root -> Other -> Next", string.Join( " -> ", node.GetPath().SelectAsArray( n => n.Label ) ) );
    }

    private class BaseWithPrivateField
    {
        private object? _value;

        public void SetBaseValue( object value ) => this._value = value;
    }

    private sealed class DerivedWithSameFieldName : BaseWithPrivateField
    {
        private object? _value;

        public void SetDerivedValue( object value ) => this._value = value;
    }

    [Fact]
    public void PrivateFieldOfBaseClass_IsFollowed()
    {
        var baseValue = new Node( "base" );
        var derivedValue = new Node( "derived" );
        var obj = new DerivedWithSameFieldName();
        obj.SetBaseValue( baseValue );
        obj.SetDerivedValue( derivedValue );

        var nodes = Walk( obj );

        // Both fields are named _value. Missing either of them would make a retention invisible.
        Assert.True( Reached( nodes, baseValue ) );
        Assert.True( Reached( nodes, derivedValue ) );
    }

    private sealed class ArrayHolder
    {
        public object[]? References;
        public int[]? Integers;
        public object[,]? Rectangular;
    }

    [Fact]
    public void ArrayOfReferences_IsFollowed()
    {
        var element = new Node( "element" );
        var holder = new ArrayHolder { References = [new Node( "other" ), element] };

        var node = Find( Walk( holder ), element );

        Assert.NotNull( node );
        Assert.Equal( "[1]", node!.Label );
    }

    [Fact]
    public void ArrayOfPrimitives_IsNotFollowed()
    {
        var holder = new ArrayHolder { Integers = [1, 2, 3] };

        var nodes = Walk( holder );

        // The array itself is discovered, but nothing inside it.
        Assert.True( Reached( nodes, holder.Integers ) );
        Assert.Equal( 2, nodes.Count );
    }

    [Fact]
    public void RectangularArray_IsNotFollowed()
    {
        var element = new Node( "element" );
        var holder = new ArrayHolder { Rectangular = new object[1, 1] };
        holder.Rectangular[0, 0] = element;

        Assert.False( Reached( Walk( holder ), element ) );
    }

    private struct Inner
    {
        public object? Value;
    }

    private struct Outer
    {
        public Inner Inner;
    }

    private sealed class StructHolder
    {
        public Outer Outer;
        public Inner[]? Array;
    }

    [Fact]
    public void FieldNestedInValueTypes_IsFollowedAndLabelled()
    {
        var value = new Node( "value" );
        var holder = new StructHolder { Outer = new Outer { Inner = new Inner { Value = value } } };

        var node = Find( Walk( holder ), value );

        Assert.NotNull( node );
        Assert.Equal( "Outer.Inner.Value", node!.Label );
    }

    [Fact]
    public void FieldOfValueTypeArrayElement_IsFollowedAndLabelled()
    {
        var value = new Node( "value" );
        var holder = new StructHolder { Array = [default, new Inner { Value = value }] };

        var node = Find( Walk( holder ), value );

        Assert.NotNull( node );
        Assert.Equal( "[1].Value", node!.Label );
    }

    private struct Box<T>
        where T : struct
    {
        public T Value;
    }

    private sealed class ShallowStructNest
    {
        public Box<Box<Box<Box<Box<Inner>>>>> Value;
    }

    private sealed class DeepStructNest
    {
        public Box<Box<Box<Box<Box<Box<Inner>>>>>> Value;
    }

    [Fact]
    public void ValueTypeNesting_IsFollowedUpToTheDepthLimit()
    {
        var reachable = new Node( "reachable" );
        var unreachable = new Node( "unreachable" );

        var shallow = new ShallowStructNest();
        shallow.Value.Value.Value.Value.Value.Value.Value = reachable;

        var deep = new DeepStructNest();
        deep.Value.Value.Value.Value.Value.Value.Value.Value = unreachable;

        Assert.True( Reached( Walk( shallow ), reachable ) );

        // Beyond the limit the walk stops descending. This bounds the cost on types such as large tuples rather than
        // expressing a rule about retention, so the loss is deliberate.
        Assert.False( Reached( Walk( deep ), unreachable ) );
    }

    private sealed class WeakHolder
    {
        public WeakReference<Node>? Weak;
        public WeakReference? Untyped;
    }

    [Fact]
    public void WeakReference_IsNotFollowed()
    {
        var target = new Node( "target" );
        var holder = new WeakHolder { Weak = new WeakReference<Node>( target ), Untyped = new WeakReference( target ) };

        // A weak reference keeps its target in a native handle, which no managed field exposes. The walker relies on
        // that rather than on a rule of its own, so this test guards the assumption.
        Assert.False( Reached( Walk( holder ), target ) );
    }

    private sealed class TableHolder
    {
        public ConditionalWeakTable<object, object>? Table;
        public object? StrongKey;
    }

    /// <summary>
    /// Verifies that the walker agrees with the runtime about whether conditional references can be followed at all.
    /// </summary>
    /// <remarks>
    /// The entries of a <see cref="ConditionalWeakTable{TKey,TValue}"/> are held through dependent handles, which
    /// reflection cannot read, so the only route to them is the enumerable interface of the table. .NET Core has one
    /// and .NET Framework has none. The tests below are conditional on this, and asserting it separately keeps the
    /// reason visible instead of leaving a bare <c>#if</c> in the middle of them.
    /// </remarks>
    [Fact]
    public void ConditionalReferences_AreFollowedOnlyWhereTheRuntimeAllowsIt()
    {
#if NET6_0_OR_GREATER
        Assert.True( ObjectGraphWalker.CanFollowConditionalReferences );
#else
        Assert.False( ObjectGraphWalker.CanFollowConditionalReferences );
#endif
    }

    [Fact]
    public void ConditionalWeakTable_FollowsTheValueOfAReachableKey()
    {
        var key = new Node( "key" );
        var value = new Node( "value" );
        var holder = new TableHolder { Table = new ConditionalWeakTable<object, object>(), StrongKey = key };
        holder.Table.Add( key, value );

        var node = Find( Walk( holder ), value );

        if ( ObjectGraphWalker.CanFollowConditionalReferences )
        {
            Assert.NotNull( node );
            Assert.True( node!.IsConditional );
        }
        else
        {
            // On .NET Framework the entries cannot be reached at all, so the value is invisible. The walk stays sound,
            // because every chain it does report is real; it is incomplete, which is what the property says.
            Assert.Null( node );
        }
    }

    [Fact]
    public void ConditionalWeakTable_DoesNotFollowTheValueOfAnUnreachableKey()
    {
        var key = new Node( "key" );
        var value = new Node( "value" );
        var holder = new TableHolder { Table = new ConditionalWeakTable<object, object>() };
        holder.Table.Add( key, value );

        var nodes = Walk( holder );

        // The table does not keep its key alive, so the entry keeps nothing alive either. Reporting the value here
        // would name a path along which the collector is free to reclaim the object.
        Assert.False( Reached( nodes, value ) );
        Assert.False( Reached( nodes, key ) );

        GC.KeepAlive( key );
    }

    [Fact]
    public void ConditionalWeakTable_DoesNotReportAnObjectRetainedByItsOwnEntry()
    {
        // The value references its own key. The collector resolves this correctly and so must the walker, otherwise it
        // would report a circular path and hide the reference that is genuinely responsible.
        var key = new Node( "key" );
        var value = new Node( "value" );
        value.Other = key;

        var holder = new TableHolder { Table = new ConditionalWeakTable<object, object>() };
        holder.Table.Add( key, value );

        Assert.False( Reached( Walk( holder ), value ) );

        GC.KeepAlive( key );
    }

    private sealed class TwoTableHolder
    {
        public ConditionalWeakTable<object, object>? First;
        public ConditionalWeakTable<object, object>? Second;
        public object? StrongKey;
    }

    [Fact]
    public void ConditionalWeakTable_ReachesAFixedPointAcrossTables()
    {
        // The value of the first entry is the key of the second. Only a walk that alternates between the strong pass
        // and the conditional pass until nothing new appears discovers the second value.
        var firstKey = new Node( "firstKey" );
        var secondKey = new Node( "secondKey" );
        var secondValue = new Node( "secondValue" );

        var holder = new TwoTableHolder
        {
            First = new ConditionalWeakTable<object, object>(), Second = new ConditionalWeakTable<object, object>(), StrongKey = firstKey
        };

        holder.First.Add( firstKey, secondKey );
        holder.Second.Add( secondKey, secondValue );

        Assert.Equal( ObjectGraphWalker.CanFollowConditionalReferences, Reached( Walk( holder ), secondValue ) );
    }

    [Fact]
    public void SkipAction_StopsTheWalkAtTheObject()
    {
        var middle = new Node( "middle" );
        var beyond = new Node( "beyond" );
        var root = new Node( "root" ) { Next = middle };
        middle.Next = beyond;

        var nodes = Walk( root, visitor: n => ReferenceEquals( n.Object, middle ) ? ObjectGraphAction.Skip : ObjectGraphAction.Traverse );

        Assert.True( Reached( nodes, middle ) );
        Assert.False( Reached( nodes, beyond ) );
    }

    [Fact]
    public void StopAction_EndsTheWalk()
    {
        var target = new Node( "target" );
        var root = new Node( "root" ) { Next = target };

        var result = new ObjectGraphWalker().Walk(
            [("root", root)],
            node => ReferenceEquals( node.Object, target ) ? ObjectGraphAction.Stop : ObjectGraphAction.Traverse );

        Assert.True( result.IsStopped );
        Assert.False( result.IsExhausted );
        Assert.False( result.IsComplete );
    }

    [Fact]
    public void ObjectBudget_ExhaustsTheWalk()
    {
        var root = new Node( "0" );
        var current = root;

        for ( var i = 1; i < 200; i++ )
        {
            current.Next = new Node( i.ToString() );
            current = current.Next;
        }

        var result = new ObjectGraphWalker( ObjectGraphWalkerOptions.Default with { MaxObjects = 10 } )
            .Walk( [("root", root)], _ => ObjectGraphAction.Traverse );

        Assert.True( result.IsExhausted );
        Assert.False( result.IsComplete );
    }

    [Fact]
    public void CompleteWalk_IsReportedAsComplete()
    {
        var result = new ObjectGraphWalker().Walk( [("root", new Node( "root" ))], _ => ObjectGraphAction.Traverse );

        Assert.True( result.IsComplete );
        Assert.Equal( 2, result.VisitedObjectCount );
    }

    [Fact]
    public void MultipleRoots_AreAllVisitedAndNamed()
    {
        var first = new Node( "first" );
        var second = new Node( "second" );

        var nodes = WalkMany( [("first", first), ("second", second)] );

        Assert.Equal( "first", Find( nodes, first )!.Label );
        Assert.Equal( "second", Find( nodes, second )!.Label );
        Assert.All( nodes.Where( n => n.Depth == 0 ), n => Assert.Null( n.Parent ) );
    }

    private sealed class RuntimeStructHolder
    {
        public object? Value;

        // A runtime value type whose layout the walker must descend into without failing.
        public Memory<byte> Memory;
    }

    [Fact]
    public void RuntimeValueTypeField_DoesNotFailTheWalk()
    {
        var buffer = new byte[4];
        var value = new Node( "value" );
        var holder = new RuntimeStructHolder { Value = value, Memory = buffer };

        var nodes = Walk( holder );

        Assert.True( Reached( nodes, value ) );
        Assert.True( Reached( nodes, buffer ) );
    }

    [Fact]
    public void FormatPath_IndentsOneHopPerLine()
    {
        var target = new Node( "target" );
        var root = new Node( "root" ) { Next = target };

        var formatted = Find( Walk( root ), target )!.FormatPath();
        var lines = formatted.Split( '\n' ).SelectAsArray( l => l.TrimEnd( '\r' ) );

        Assert.Equal( 2, lines.Length );
        Assert.Equal( "root : Node", lines[0] );
        Assert.Equal( "  Next : Node", lines[1] );
    }

    [Fact]
    public void FormatType_RendersGenericArguments()
    {
        Assert.Equal( "Node", ObjectGraphNode.FormatType( typeof(Node) ) );
        Assert.Equal( "Dictionary<String,Node>", ObjectGraphNode.FormatType( typeof(Dictionary<string, Node>) ) );
        Assert.Equal( "List<Dictionary<String,Node>>", ObjectGraphNode.FormatType( typeof(List<Dictionary<string, Node>>) ) );
    }

#pragma warning restore SA1401, CS0649, IDE0044, IDE0051, RS0030
}
