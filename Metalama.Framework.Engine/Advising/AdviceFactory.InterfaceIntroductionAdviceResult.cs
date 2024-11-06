// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.Advising;

internal sealed partial class AdviceFactory<T>
    where T : IDeclaration
{
    private sealed class ClassIntroductionAdviceResult : IClassIntroductionAdviceResult, IAdviserInternal
    {
        private readonly AdviceFactory<T> _origin;
        private readonly IIntroductionAdviceResult<INamedType> _inner;

        public ClassIntroductionAdviceResult( AdviceFactory<T> origin, IIntroductionAdviceResult<INamedType> inner )
        {
            this._origin = origin;
            this._inner = inner;
        }

        public INamedType Declaration => this._inner.Declaration;

        public IDeclaration ConflictingDeclaration => this._inner.ConflictingDeclaration;

        public AdviceKind AdviceKind => this._inner.AdviceKind;

        public AdviceOutcome Outcome => this._inner.Outcome;

        public INamedType Target => this._inner.Declaration;

        public IAdviceFactory AdviceFactory => this._origin;

        public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
            where TNewDeclaration : IDeclaration
            => this._origin.WithDeclaration( declaration );
    }
}