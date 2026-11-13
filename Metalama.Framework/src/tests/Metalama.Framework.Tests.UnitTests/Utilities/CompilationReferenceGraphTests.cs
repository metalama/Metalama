// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// Tests the depth analysis of <see cref="CompilationReferenceGraph"/> and validates its complexity with <see cref="CompilationReferenceGraphObserver"/>.
/// See https://github.com/metalama/Metalama/issues/2330.
/// </summary>
/// <remarks>
/// <para>
/// Notation: the reference graph is a directed acyclic graph whose root is the compilation. V is the number of assemblies, E is the number
/// of references, L is the length of the longest path, and [Min(v), Max(v)] is the depth range of the assembly v.
/// Paths(v) is the number of distinct paths from the root to v.
/// </para>
/// <para>
/// Without pruning, the analysis enumerates every path from the root, so it visits each assembly v exactly Paths(v) times.
/// This number is exponential in the number of diamonds of the graph.
/// </para>
/// <para>
/// With pruning, the analysis enumerates the references of an assembly only when its depth is outside the range already recorded.
/// Each such enumeration widens the range by at least one, so the references of v are enumerated at most 1 + Max(v) - Min(v) times.
/// Each enumeration visits each reference once, so the number of visits is 1 + sum( walks(v) * outdegree(v) ), which is at most
/// 1 + sum( (1 + Max(v) - Min(v)) * outdegree(v) ), that is O( L * E ).
/// </para>
/// </remarks>
public sealed class CompilationReferenceGraphTests : UnitTestClass
{
    private const string _rootName = "Root";

    /// <summary>
    /// Initializes a new instance of the <see cref="CompilationReferenceGraphTests"/> class.
    /// </summary>
    public CompilationReferenceGraphTests( ITestOutputHelper testOutputHelper ) : base( testOutputHelper ) { }

    /// <summary>
    /// Gets the names of the graphs that <see cref="GetGraph"/> creates.
    /// </summary>
    public static TheoryData<string> GraphNames => new() { "DiamondChain1", "DiamondChain2", "DiamondChain5", "Chain6", "ReversedChain6", "Issue2330" };

    /// <summary>
    /// Verifies that the depth range of each assembly is the length of the shortest and of the longest path from the root.
    /// </summary>
    [Theory]
    [MemberData( nameof(GraphNames) )]
    public void DepthsAreLengthsOfShortestAndLongestPaths( string graphName )
    {
        var graph = GetGraph( graphName );
        var result = this.Analyze( graph );
        var oracle = new Oracle( graph );

        foreach ( var node in graph.Nodes )
        {
            Assert.Equal( (oracle.MinDepth[node], oracle.MaxDepth[node]), result.Depths[node] );
        }
    }

    /// <summary>
    /// Verifies that the references of each assembly are enumerated at most 1 + Max - Min times, and that each enumeration visits each reference once.
    /// </summary>
    [Theory]
    [MemberData( nameof(GraphNames) )]
    public void ReferencesAreWalkedOncePerDepthRangeIncrement( string graphName )
    {
        var graph = GetGraph( graphName );
        var result = this.Analyze( graph );
        var oracle = new Oracle( graph );

        foreach ( var node in graph.Nodes )
        {
            var walks = result.Observer.GetExpansionCount( node );

            Assert.InRange( walks, 1, 1 + oracle.MaxDepth[node] - oracle.MinDepth[node] );
        }

        // Each walk of the references of v visits each reference once.
        var expectedVisits = 1 + graph.Nodes.Sum( n => result.Observer.GetExpansionCount( n ) * graph.GetReferences( n ).Count );

        Assert.Equal( expectedVisits, result.Observer.VisitCount );
    }

    /// <summary>
    /// Verifies that the number of visits is O( L * E ) on chains of diamonds, where the number of paths is exponential.
    /// The test output gives the number of paths, the number of visits, and the theoretical bound for each size.
    /// </summary>
    [Fact]
    public void VisitCountIsPolynomialWhenPathCountIsExponential()
    {
        this.TestOutput.WriteLine( $"{"k",4} {"V",5} {"E",7} {"L",4} {"Paths (original)",20} {"Visits",8} {"Bound",8}" );

        foreach ( var k in new[] { 1, 2, 4, 8, 16, 32, 64, 128 } )
        {
            var graph = Graph.CreateDiamondChain( k );
            var result = this.Analyze( graph );
            var oracle = new Oracle( graph );

            var bound = 1 + graph.Nodes.Sum( n => (1 + oracle.MaxDepth[n] - oracle.MinDepth[n]) * graph.GetReferences( n ).Count );
            var longestPath = oracle.MaxDepth.Values.Max();

            this.TestOutput.WriteLine(
                $"{k,4} {graph.Nodes.Count,5} {graph.EdgeCount,7} {longestPath,4} {oracle.TotalPathCount,20:E3} {result.Observer.VisitCount,8} {bound,8}" );

            Assert.InRange( result.Observer.VisitCount, 1, bound );

            // O( L * E ) with L <= V.
            Assert.InRange( result.Observer.VisitCount, 1, (longestPath + 1) * (graph.EdgeCount + 1) );
        }
    }

    /// <summary>
    /// Creates the graph of the given name.
    /// </summary>
    private static Graph GetGraph( string name )
        => name switch
        {
            "DiamondChain1" => Graph.CreateDiamondChain( 1 ),
            "DiamondChain2" => Graph.CreateDiamondChain( 2 ),
            "DiamondChain5" => Graph.CreateDiamondChain( 5 ),
            "Chain6" => Graph.CreateChain( 6, reverseRootReferences: false ),
            "ReversedChain6" => Graph.CreateChain( 6, reverseRootReferences: true ),
            "Issue2330" => Graph.CreateIssue2330(),
            _ => throw new ArgumentOutOfRangeException( nameof(name) )
        };

    /// <summary>
    /// Builds the compilation of the graph and runs the depth analysis with an observer.
    /// </summary>
    private (Dictionary<string, (int Min, int Max)> Depths, CompilationReferenceGraphObserver Observer) Analyze( Graph graph )
    {
        var observer = new CompilationReferenceGraphObserver();
        var additionalServices = new AdditionalServiceCollection();
        additionalServices.AddProjectService( observer );
        using var testContext = this.CreateTestContext( additionalServices );

        var compilation = graph.CreateCompilation();
        var referenceGraph = CompilationReferenceGraph.GetInstance( compilation, testContext.ServiceProvider );

        var assemblies = compilation.SourceModule.ReferencedAssemblySymbols.Where( a => graph.Nodes.Contains( a.Name ) ).Append( compilation.Assembly );
        var depths = assemblies.ToDictionary( a => a.Name, a => referenceGraph.GetDepth( a ) );

        return (depths, observer);
    }

    /// <summary>
    /// A DAG of assemblies. The root is the compilation and references all other assemblies, like a project compilation references the transitive
    /// closure of its packages.
    /// </summary>
    private sealed class Graph
    {
        private readonly Dictionary<string, List<string>> _references = new();

        /// <summary>
        /// Gets the nodes in reverse topological order: each node is added after the nodes that it references.
        /// </summary>
        public List<string> Nodes { get; } = [];

        /// <summary>
        /// Gets the number of references in the graph.
        /// </summary>
        public int EdgeCount => this._references.Values.Sum( r => r.Count );

        /// <summary>
        /// Gets the assemblies that the given node references.
        /// </summary>
        public IReadOnlyList<string> GetReferences( string node ) => this._references[node];

        private void Add( string node, params string[] references )
        {
            this._references.Add( node, references.ToMutableList() );
            this.Nodes.Add( node );
        }

        private void AddRoot( bool reverse )
        {
            var references = this.Nodes.ToArray();

            if ( reverse )
            {
                Array.Reverse( references );
            }

            this.Add( _rootName, references );
        }

        /// <summary>
        /// Creates the chain of k diamonds Ck -> {Ak, Bk} -> C(k-1) -> ... -> {A1, B1} -> C0. There are 2^(k-i) paths from Ck to Ci.
        /// </summary>
        public static Graph CreateDiamondChain( int k )
        {
            var graph = new Graph();
            graph.Add( "C0" );

            for ( var i = 1; i <= k; i++ )
            {
                graph.Add( $"A{i}", $"C{i - 1}" );
                graph.Add( $"B{i}", $"C{i - 1}" );
                graph.Add( $"C{i}", $"A{i}", $"B{i}" );
            }

            // The references of the root are reversed, so the deepest assembly is visited first and every other assembly is first
            // visited at a small depth. This order requires the largest number of enumerations.
            graph.AddRoot( reverse: true );

            return graph;
        }

        /// <summary>
        /// Creates the chain Nn -> ... -> N2 -> N1.
        /// </summary>
        public static Graph CreateChain( int n, bool reverseRootReferences )
        {
            var graph = new Graph();
            graph.Add( "N1" );

            for ( var i = 2; i <= n; i++ )
            {
                graph.Add( $"N{i}", $"N{i - 1}" );
            }

            graph.AddRoot( reverseRootReferences );

            return graph;
        }

        /// <summary>
        /// Creates a graph where F is reached twice at the same depth and once at a greater depth, as in the diagram of the issue.
        /// </summary>
        public static Graph CreateIssue2330()
        {
            var graph = new Graph();
            graph.Add( "H" );
            graph.Add( "G" );
            graph.Add( "F", "G", "H" );
            graph.Add( "E", "F" );
            graph.Add( "D", "F" );
            graph.Add( "C", "F" );
            graph.Add( "B", "D", "E" );
            graph.Add( "A", "B", "C" );
            graph.AddRoot( reverse: true );

            return graph;
        }

        /// <summary>
        /// Emits an assembly for each node except the root, and creates the compilation of the root, which references every other node.
        /// </summary>
        public Compilation CreateCompilation()
        {
            var coreLibrary = MetadataReference.CreateFromFile( typeof(object).Assembly.Location );
            var images = new Dictionary<string, MetadataReference>();

            foreach ( var node in this.Nodes.Where( n => n != _rootName ) )
            {
                // The type must use a type of each referenced assembly, otherwise the compiler does not emit the assembly reference.
                var fields = string.Concat( this._references[node].Select( ( r, i ) => $" public T{r} F{i};" ) );

                var compilation = CSharpCompilation.Create(
                    node,
                    [CSharpSyntaxTree.ParseText( $"public class T{node} {{{fields} }}" )],
                    this._references[node].SelectAsArray( r => images[r] ).Append( coreLibrary ),
                    new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );

                using var stream = new MemoryStream();
                var emitResult = compilation.Emit( stream );

                Assert.True( emitResult.Success, string.Join( Environment.NewLine, emitResult.Diagnostics ) );

                images.Add( node, MetadataReference.CreateFromImage( stream.ToArray() ) );
            }

            return CSharpCompilation.Create(
                _rootName,
                references: this._references[_rootName].SelectAsArray( r => images[r] ).Append( coreLibrary ),
                options: new CSharpCompilationOptions( OutputKind.DynamicallyLinkedLibrary ) );
        }
    }

    /// <summary>
    /// Computes the expected depths and the number of paths with a dynamic program in topological order, in O( V + E ).
    /// </summary>
    private sealed class Oracle
    {
        /// <summary>
        /// Gets the length of the shortest path from the root to each node.
        /// </summary>
        public Dictionary<string, int> MinDepth { get; } = new();

        /// <summary>
        /// Gets the length of the longest path from the root to each node.
        /// </summary>
        public Dictionary<string, int> MaxDepth { get; } = new();

        /// <summary>
        /// Gets the number of distinct paths from the root to each node.
        /// </summary>
        public Dictionary<string, BigInteger> PathCount { get; } = new();

        /// <summary>
        /// Gets the number of visits of the analysis without pruning, which enumerates each path from the root.
        /// </summary>
        public BigInteger TotalPathCount => this.PathCount.Values.Aggregate( BigInteger.Zero, ( x, y ) => x + y );

        /// <summary>
        /// Initializes a new instance of the <see cref="Oracle"/> class.
        /// </summary>
        public Oracle( Graph graph )
        {
            this.MinDepth[_rootName] = 0;
            this.MaxDepth[_rootName] = 0;
            this.PathCount[_rootName] = 1;

            // Nodes are in reverse topological order, so we visit them from the root.
            foreach ( var node in Enumerable.Reverse( graph.Nodes ) )
            {
                foreach ( var reference in graph.GetReferences( node ) )
                {
                    this.MinDepth[reference] = Math.Min( this.MinDepth.TryGetValue( reference, out var min ) ? min : int.MaxValue, this.MinDepth[node] + 1 );
                    this.MaxDepth[reference] = Math.Max( this.MaxDepth.TryGetValue( reference, out var max ) ? max : int.MinValue, this.MaxDepth[node] + 1 );
                    this.PathCount[reference] = (this.PathCount.TryGetValue( reference, out var count ) ? count : 0) + this.PathCount[node];
                }
            }
        }
    }
}
