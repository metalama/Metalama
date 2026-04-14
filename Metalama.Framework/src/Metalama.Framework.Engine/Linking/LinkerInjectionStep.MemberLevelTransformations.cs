// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.SyntaxGeneration;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    // ReSharper disable once ClassNeverInstantiated.Local
    private sealed class MemberLevelTransformations
    {
        // TODO: this class is no longer used concurrently, and is being added in transformation order.

        private ConcurrentLinkedList<IntroduceParameterTransformation>? _unorderedParameters;
        private ConcurrentLinkedList<IntroduceConstructorInitializerArgumentTransformation>? _unorderedArguments;

        public ImmutableArray<IntroduceParameterTransformation> Parameters { get; private set; }

        public ImmutableArray<IntroduceConstructorInitializerArgumentTransformation> Arguments { get; private set; }

        public void Sort()
        {
            if ( this._unorderedArguments == null )
            {
                this.Arguments = ImmutableArray<IntroduceConstructorInitializerArgumentTransformation>.Empty;
            }
            else
            {
                // ConcurrentLinkedList.ToList() returns items in reverse insertion order (LIFO), so the
                // first element here is the latest-added transformation.
                var all = this._unorderedArguments.ToList();

                // When an initializer-argument transformation has IsOverride == true it supersedes any
                // earlier-appended transformation targeting the same parameter index. This is used by the
                // OnConstructed advice to rewrite the `context` argument pulled into a derived `:base(...)`
                // call so it descends with InitializationSlot.OnConstructed.
                var overridesByIndex = all
                    .Where( a => a.IsOverride )
                    .GroupBy( a => a.ParameterIndex )
                    .ToDictionary( g => g.Key, g => g.First() );

                IEnumerable<IntroduceConstructorInitializerArgumentTransformation> filtered;

                if ( overridesByIndex.Count == 0 )
                {
                    filtered = all;
                }
                else
                {
                    filtered = all
                        .Where( a => !overridesByIndex.ContainsKey( a.ParameterIndex ) )
                        .Concat( overridesByIndex.Values );
                }

                this.Arguments = filtered.OrderBy( a => a.ParameterIndex ).ToImmutableArray();
            }

            if ( this._unorderedParameters == null )
            {
                this.Parameters = ImmutableArray<IntroduceParameterTransformation>.Empty;
            }
            else
            {
                var allParams = this._unorderedParameters.ToList();

                // When a parameter transformation has IsReplacement == true it supersedes the original
                // IntroduceParameterTransformation at the same parameter index. This is used when
                // a derived aspect replaces an introduced parameter's type with a more specific one.
                var replacementsByIndex = allParams
                    .Where( p => p.IsReplacement )
                    .GroupBy( p => p.Parameter.Index )
                    .ToDictionary( g => g.Key, g => g.First() );

                IEnumerable<IntroduceParameterTransformation> filteredParams;

                if ( replacementsByIndex.Count == 0 )
                {
                    filteredParams = allParams;
                }
                else
                {
                    filteredParams = allParams
                        .Where( p => !p.IsReplacement && !replacementsByIndex.ContainsKey( p.Parameter.Index ) )
                        .Concat( replacementsByIndex.Values );
                }

                this.Parameters = filteredParams.OrderBy( p => p.Parameter.Index ).ToImmutableArray();
            }

            // Resolve ForwardParameterName hints against the final parameter list for this target
            // constructor. When an initializer argument was emitted with a hint (see PullConstructorParameterAdviceImpl
            // UseExpression branch) and the target's final parameters contain an aspect-introduced parameter
            // with that name, rewrite the argument value to forward that parameter's identifier. This makes
            // the output independent of the order in which sibling IntroduceParameter advice calls were issued
            // on ctors in a :this chain. When no match exists (e.g. the target is a forwarding constructor
            // that carries only source-compatibility parameters), the original fallback expression is retained.
            if ( this.Arguments.Length > 0 && this.Parameters.Length > 0 )
            {
                ImmutableArray<IntroduceConstructorInitializerArgumentTransformation>.Builder? resolved = null;

                for ( var i = 0; i < this.Arguments.Length; i++ )
                {
                    var arg = this.Arguments[i];

                    if ( arg.ForwardParameterName == null )
                    {
                        resolved?.Add( arg );

                        continue;
                    }

                    IntroduceParameterTransformation? match = null;

                    foreach ( var p in this.Parameters )
                    {
                        if ( p.Parameter.Name == arg.ForwardParameterName )
                        {
                            match = p;

                            break;
                        }
                    }

                    if ( match != null )
                    {
                        if ( resolved == null )
                        {
                            resolved = ImmutableArray.CreateBuilder<IntroduceConstructorInitializerArgumentTransformation>( this.Arguments.Length );

                            for ( var j = 0; j < i; j++ )
                            {
                                resolved.Add( this.Arguments[j] );
                            }
                        }

                        resolved.Add( arg.WithResolvedValue( SyntaxFactoryEx.SafeIdentifierName( match.Parameter.Name.AssertNotNull() ) ) );
                    }
                    else
                    {
                        resolved?.Add( arg );
                    }
                }

                if ( resolved != null )
                {
                    this.Arguments = resolved.ToImmutable();
                }
            }
        }

        public void Add( IntroduceParameterTransformation transformation )
            => LazyInitializer.EnsureInitialized( ref this._unorderedParameters ).Add( transformation );

        public void Add( IntroduceConstructorInitializerArgumentTransformation argument )
            => LazyInitializer.EnsureInitialized( ref this._unorderedArguments ).Add( argument );
    }
}