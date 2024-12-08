// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Engine.Advising;

internal sealed partial class AdviceFactory<T>
    where T : IDeclaration
{
    private sealed class NamespaceIntroductionAdviceResult : INamespaceIntroductionAdviceResult, IAdviserInternal
    {
        private readonly AdviceFactory<T> _origin;
        private readonly IIntroductionAdviceResult<INamespace> _inner;

        public NamespaceIntroductionAdviceResult( AdviceFactory<T> origin, IIntroductionAdviceResult<INamespace> inner )
        {
            this._origin = origin;
            this._inner = inner;
        }

        public INamespace Declaration => this._inner.Declaration;

        public IDeclaration ConflictingDeclaration => this._inner.ConflictingDeclaration;

        public AdviceKind AdviceKind => this._inner.AdviceKind;

        public AdviceOutcome Outcome => this._inner.Outcome;

        public ScopedDiagnosticSink Diagnostics => this._origin.Diagnostics;

        IDeclaration IAdviser.Target => this.Target;

        public INamespace Target => this._inner.Declaration;

        public IAdviceFactory AdviceFactory => this._origin;

        public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
            where TNewDeclaration : class, IDeclaration
            => this._origin.WithDeclaration( declaration );
    }
}