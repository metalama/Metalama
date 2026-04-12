// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.AdviceImpl.Introduction.Constructors;
using Metalama.Framework.Engine.Collections;
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

            this.Parameters = this._unorderedParameters?.OrderBy( p => p.Parameter.Index ).ToImmutableArray()
                              ?? ImmutableArray<IntroduceParameterTransformation>.Empty;
        }

        public void Add( IntroduceParameterTransformation transformation )
            => LazyInitializer.EnsureInitialized( ref this._unorderedParameters ).Add( transformation );

        public void Add( IntroduceConstructorInitializerArgumentTransformation argument )
            => LazyInitializer.EnsureInitialized( ref this._unorderedArguments ).Add( argument );
    }
}