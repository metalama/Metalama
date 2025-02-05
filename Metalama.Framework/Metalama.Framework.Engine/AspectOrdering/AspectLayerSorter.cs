// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AspectOrdering;

/// <summary>
/// Compares and sorts dependency objects.
/// </summary>
internal static class AspectLayerSorter
{
    public static bool TrySort(
        IReadOnlyCollection<IAspectClassImpl> aspectClasses,
        IReadOnlyList<IAspectOrderingSource> aspectOrderingSources,
        IDiagnosticAdder diagnosticAdder,
        out ImmutableArray<OrderedAspectLayer> sortedAspectLayers )
        => TrySort(
            aspectClasses,
            aspectOrderingSources.SelectMany( s => s.GetAspectOrderSpecification( diagnosticAdder ) ).ToImmutableArray(),
            diagnosticAdder,
            out sortedAspectLayers );

    private static bool TrySort(
        IReadOnlyCollection<IAspectClassImpl> aspectClasses,
        IReadOnlyList<AspectOrderSpecification> relationships,
        IDiagnosticAdder diagnosticAdder,
        out ImmutableArray<OrderedAspectLayer> sortedAspectLayers )
    {
        var unsortedAspectLayers = aspectClasses
            .Where( t => !t.IsAbstract )
            .SelectMany( at => at.Layers )
            .ToReadOnlyList();

        var aspectsByName = aspectClasses.ToDictionary( a => a.FullName, a => a );

        // Build a graph of dependencies between unordered transformations.
        var n = unsortedAspectLayers.Count;

        var partNameToIndexMapping =
            unsortedAspectLayers
                .Select( ( t, i ) => (t.AspectLayerId.FullName, Index: i) )
                .ToDictionary( x => x.FullName, x => x.Index );

        var aspectNameToIndicesMapping =
            unsortedAspectLayers
                .Select( ( t, i ) => (t.AspectName, Index: i) )
                .ToMultiValueDictionary( p => p.AspectName, p => p.Index );

        var aspectLayerNameToLocationsMappingBuilder = ImmutableDictionaryOfArray<string, AspectOrderSpecification>.CreateBuilder();

        DirectedGraph directedGraph = new( n );
        var hasPredecessor = new bool[n];

        foreach ( var relationship in relationships )
        {
            var previousIndices = ImmutableArray<int>.Empty;

            foreach ( var matchExpression in relationship.OrderedLayers )
            {
                var indexOfColon = matchExpression.IndexOfOrdinal( ':' );

                string aspectName;
                string? layerName;

                if ( indexOfColon < 0 )
                {
                    aspectName = matchExpression;
                    layerName = null;
                }
                else if ( indexOfColon == 0 )
                {
                    throw new AssertionFailedException();
                }
                else
                {
                    aspectName = matchExpression.Substring( 0, indexOfColon );
                    layerName = matchExpression.Substring( indexOfColon + 1 );
                }

                if ( !aspectsByName.TryGetValue( aspectName, out var aspect ) )
                {
                    continue;
                }

                var currentIndices = ImmutableArray.CreateBuilder<int>();

                var affectedAspects = relationship.ApplyToDerivedTypes 
                    ? aspect.DescendantClassesAndSelf.Where( c => !c.IsAbstract ) 
                    : [aspect];

                foreach ( var descendant in affectedAspects )
                {
                    // Map the part string to a set of indices. We don't require the match to exist because it is a normal case
                    // to specify ordering for aspects that exist but are not necessarily a part of the current compilation.

                    if ( layerName == "*" )
                    {
                        currentIndices.AddRange( aspectNameToIndicesMapping[descendant.FullName] );
                    }
                    else
                    {
                        var descendentLayer = layerName != null ? descendant.FullName + ":" + layerName : descendant.FullName;

                        if ( partNameToIndexMapping.TryGetValue( descendentLayer, out var currentIndex ) )
                        {
                            currentIndices.Add( currentIndex );
                        }
                    }
                }

                if ( currentIndices.Count > 0 )
                {
                    if ( !previousIndices.IsEmpty )
                    {
                        // Index the relationship so we can later resolve locations.
                        aspectLayerNameToLocationsMappingBuilder.AddRange(
                            currentIndices,
                            i => unsortedAspectLayers[i].AspectLayerId.FullName,
                            _ => relationship );

                        // Add edges to previous nodes.
                        foreach ( var previousIndex in previousIndices )
                        {
                            foreach ( var currentIndex in currentIndices )
                            {
                                directedGraph.AddEdge( previousIndex, currentIndex );
                                hasPredecessor[currentIndex] = true;
                            }
                        }
                    }

                    previousIndices = currentIndices.ToImmutable();
                }
            }
        }

        var aspectLayerNameToLocationsMapping = aspectLayerNameToLocationsMappingBuilder.ToImmutable();

        // Perform a breadth-first search on the graph.
        var distances = directedGraph.GetInitialVector();
        var predecessors = directedGraph.GetInitialVector();

        var cycle = -1;

        for ( var i = 0; i < n; i++ )
        {
            if ( !hasPredecessor[i] )
            {
                cycle = directedGraph.DoBreadthFirstSearch( i, distances, predecessors );

                if ( cycle >= 0 )
                {
                    break;
                }
            }
        }

        // If we did not find any cycle, we need to check that we have ordered the whole graph.
        if ( cycle < 0 )
        {
            for ( var i = 0; i < n; i++ )
            {
                if ( distances[i] == DirectedGraph.NotDiscovered )
                {
                    // There is a node that we haven't ordered, which means that there is a cycle.
                    // Force the detection on the node to find the cycle.
                    cycle = directedGraph.DoBreadthFirstSearch( 0, distances, predecessors );

                    break;
                }
            }
        }

        // Detect cycles.
        if ( cycle >= 0 )
        {
            // Build a string containing the unITransformations of the cycle.
            Stack<int> cycleStack = new( unsortedAspectLayers.Count );

            var cursor = cycle;

            do
            {
                cycleStack.Push( cursor );
                cursor = predecessors[cursor];
            }
            while ( cursor != cycle && /* Workaround PostSharp bug 25438 */ cursor != DirectedGraph.NotDiscovered );

            var cycleNodes = cycleStack.SelectAsImmutableArray( index => unsortedAspectLayers[index].AspectLayerId.FullName );

            var cycleLocations = cycleNodes
                .SelectMany( c => aspectLayerNameToLocationsMapping[c] )
                .Select( s => s.DiagnosticLocation )
                .WhereNotNull()
                .GroupBy( l => l )
                .OrderByDescending( g => g.Count() )
                .Select( g => g.Key )
                .ToReadOnlyList();

            var mainLocation = cycleLocations.FirstOrDefault();
            var additionalLocations = cycleLocations.Skip( 1 );

            var cycleNodesString = string.Join( ", ", cycleNodes );

            var diagnostic =
                GeneralDiagnosticDescriptors.CycleInAspectOrdering.CreateRoslynDiagnostic(
                    mainLocation,
                    cycleNodesString,
                    null,
                    additionalLocations );

            diagnosticAdder.Report( diagnostic );
            sortedAspectLayers = default;

            return false;
        }

        // Sort the distances vector.
        var sortedIndexes = new int[n];

        for ( var i = 0; i < n; i++ )
        {
            sortedIndexes[i] = i;
        }

        Array.Sort(
            sortedIndexes,
            ( i, j ) =>
            {
                if ( i == j )
                {
                    return 0;
                }

                var compareDistance = distances[i].CompareTo( distances[j] );

                if ( compareDistance != 0 )
                {
                    return compareDistance;
                }

                // If two aspects are not explicitly ordered, we order them alphabetically.
                var compareName = StringComparer.Ordinal.Compare( unsortedAspectLayers[i].AspectName, unsortedAspectLayers[j].AspectName );

                if ( compareName != 0 )
                {
                    return -1 * compareName;
                }

                // At this stage, all aspects should be ordered.
                throw new AssertionFailedException( $"Nodes '{unsortedAspectLayers[i]}' and '{unsortedAspectLayers[j]}' are not sorted." );
            } );

        // Build the ordered list of aspects and assign the distance.
        // Note that we don't detect cycles because some aspect types that are present in the compilation may actually be unused.
        var sortedAspectLayersBuilder = ImmutableArray.CreateBuilder<OrderedAspectLayer>( n );

        for ( var i = 0; i < n; i++ )
        {
            var explicitOrder = distances[sortedIndexes[i]];
            sortedAspectLayersBuilder.Add( new OrderedAspectLayer( i, explicitOrder, unsortedAspectLayers[sortedIndexes[i]] ) );
        }

        sortedAspectLayers = sortedAspectLayersBuilder.ToImmutable();

        return true;
    }
}