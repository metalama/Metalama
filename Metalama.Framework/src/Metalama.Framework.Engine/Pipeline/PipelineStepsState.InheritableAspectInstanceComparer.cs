// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Comparers;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline
{
    internal sealed partial class PipelineStepsState
    {
        /// <summary>
        /// Orders the inheritable aspect instances of a pipeline result in a deterministic order, so that the transitive
        /// aspect manifest built from them is identical for identical source (#2248).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The instances are ordered by the full name of their aspect class, then by their target declaration according to
        /// <see cref="DeclarationOrderingComparer"/>, then by their predecessors. Two predecessors are compared by their
        /// kind, then by their target declaration, then by the source location of the predecessor, then recursively by
        /// their own predecessors. The predecessors distinguish, for example, two instances of one aspect class that a type
        /// inherits from two base interfaces.
        /// </para>
        /// <para>
        /// The comparer returns zero only for instances that have the same aspect class, the same target declaration and the
        /// same chain of predecessors. Such instances are added by a single producer, which adds them sequentially, so
        /// their relative order in the input is already deterministic. The caller must therefore use a stable sort.
        /// </para>
        /// </remarks>
        private sealed class InheritableAspectInstanceComparer : IComparer<IAspectInstance>
        {
            private readonly CompilationModel _compilation;

            public InheritableAspectInstanceComparer( CompilationModel compilation )
            {
                this._compilation = compilation;
            }

            public int Compare( IAspectInstance? x, IAspectInstance? y )
            {
                if ( ReferenceEquals( x, y ) )
                {
                    return 0;
                }

                if ( x == null )
                {
                    return -1;
                }

                if ( y == null )
                {
                    return 1;
                }

                var result = string.CompareOrdinal( x.AspectClass.FullName, y.AspectClass.FullName );

                if ( result != 0 )
                {
                    return result;
                }

                return this.ComparePredecessors( x, y );
            }

            /// <summary>
            /// Compares two predecessors by their target declaration, then by their source location, then recursively by their
            /// own predecessors.
            /// </summary>
            private int ComparePredecessors( IAspectPredecessor x, IAspectPredecessor y )
            {
                if ( ReferenceEquals( x, y ) )
                {
                    return 0;
                }

                var result = DeclarationOrderingComparer.Instance.Compare(
                    x.TargetDeclaration.GetTargetOrNull( this._compilation ),
                    y.TargetDeclaration.GetTargetOrNull( this._compilation ) );

                if ( result != 0 )
                {
                    return result;
                }

                result = this.CompareLocations( x, y );

                if ( result != 0 )
                {
                    return result;
                }

                return this.ComparePredecessorLists( x.Predecessors, y.Predecessors );
            }

            private int ComparePredecessorLists( ImmutableArray<AspectPredecessor> x, ImmutableArray<AspectPredecessor> y )
            {
                var xCount = x.IsDefault ? 0 : x.Length;
                var yCount = y.IsDefault ? 0 : y.Length;

                for ( var i = 0; i < Math.Min( xCount, yCount ); i++ )
                {
                    // PERF: Cast enum to int otherwise it will be boxed on .NET Framework.
                    var result = ((int) x[i].Kind).CompareTo( (int) y[i].Kind );

                    if ( result != 0 )
                    {
                        return result;
                    }

                    result = this.ComparePredecessors( x[i].Instance, y[i].Instance );

                    if ( result != 0 )
                    {
                        return result;
                    }
                }

                return xCount.CompareTo( yCount );
            }

            /// <summary>
            /// Compares the source locations of two predecessors by the path of their syntax tree, then by their position.
            /// </summary>
            /// <remarks>
            /// The location distinguishes two attributes of the same aspect class on one declaration, and two fabrics that
            /// target one declaration. A predecessor without a source location precedes one with a source location.
            /// </remarks>
            private int CompareLocations( IAspectPredecessor x, IAspectPredecessor y )
            {
                var xLocation = (x as IAspectPredecessorImpl)?.GetDiagnosticLocation( this._compilation.RoslynCompilation );
                var yLocation = (y as IAspectPredecessorImpl)?.GetDiagnosticLocation( this._compilation.RoslynCompilation );

                var xTree = xLocation?.SourceTree;
                var yTree = yLocation?.SourceTree;

                if ( xTree == null || yTree == null )
                {
                    return (xTree != null).CompareTo( yTree != null );
                }

                var result = string.CompareOrdinal( xTree.FilePath, yTree.FilePath );

                if ( result != 0 )
                {
                    return result;
                }

                return xLocation!.SourceSpan.Start.CompareTo( yLocation!.SourceSpan.Start );
            }
        }
    }
}
