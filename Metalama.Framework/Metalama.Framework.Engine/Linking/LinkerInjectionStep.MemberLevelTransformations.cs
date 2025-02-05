// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.Collections;
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
            this.Arguments = this._unorderedArguments?.OrderBy( a => a.ParameterIndex ).ToImmutableArray()
                             ?? ImmutableArray<IntroduceConstructorInitializerArgumentTransformation>.Empty;

            this.Parameters = this._unorderedParameters?.OrderBy( p => p.Parameter.Index ).ToImmutableArray()
                              ?? ImmutableArray<IntroduceParameterTransformation>.Empty;
        }

        public void Add( IntroduceParameterTransformation transformation )
            => LazyInitializer.EnsureInitialized( ref this._unorderedParameters ).Add( transformation );

        public void Add( IntroduceConstructorInitializerArgumentTransformation argument )
            => LazyInitializer.EnsureInitialized( ref this._unorderedArguments ).Add( argument );
    }
}