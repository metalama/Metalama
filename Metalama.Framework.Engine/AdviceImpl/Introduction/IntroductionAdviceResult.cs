// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroductionAdviceResult<T> : AdviceResult, IIntroductionAdviceResult<T>, IAdviserInternal
    where T : class, IDeclaration
{
    private readonly IAdviceFactoryImpl? _adviceFactory;
    private readonly IRef<T>? _declaration;
    private readonly IRef<IDeclaration>? _conflictingDeclaration;

    public IntroductionAdviceResult(
        AdviceKind adviceKind,
        AdviceOutcome outcome,
        IRef<T>? declaration,
        IRef<IDeclaration>? conflictingDeclaration,
        IAdviceFactoryImpl adviceFactory )
    {
        this.Outcome = outcome;
        this.AdviceKind = adviceKind;
        this._declaration = declaration;
        this._conflictingDeclaration = conflictingDeclaration;
        this._adviceFactory = adviceFactory;
    }

    public IntroductionAdviceResult() { }

    [Memo]
    public T Declaration => this.Resolve( this._declaration );

    [Memo]
    public IDeclaration ConflictingDeclaration => this.Resolve( this._conflictingDeclaration );

    ScopedDiagnosticSink IAdviser.Diagnostics => this._adviceFactory?.Diagnostics ?? throw new InvalidOperationException();

    public T Target => this.Declaration;

    IDeclaration IAdviser.Target => this.Target;

    public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
        where TNewDeclaration : class, IDeclaration
        => this._adviceFactory?.WithDeclaration( declaration ) ?? throw new InvalidOperationException();

    IAdviceFactory IAdviserInternal.AdviceFactory => this._adviceFactory ?? throw new InvalidOperationException();
}