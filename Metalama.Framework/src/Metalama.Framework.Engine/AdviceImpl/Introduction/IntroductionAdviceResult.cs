// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Introduction;

internal sealed class IntroductionAdviceResult<T> : AdviceResult, IIntroductionAdviceResult<T>, IAdviserInternal
    where T : class, IDeclaration
{
    private readonly IRef<T>? _declaration;
    private readonly IRef<IDeclaration>? _conflictingDeclaration;

    public IntroductionAdviceResult(
        AdviceKind adviceKind,
        AdviceOutcome outcome,
        IAdviceFactoryImpl adviceFactory,
        IRef<T>? declaration = null,
        IRef<IDeclaration>? conflictingDeclaration = null,
        ImmutableArray<Diagnostic> reportedDiagnostics = default ) : base( adviceKind, outcome, adviceFactory, reportedDiagnostics )
    {
        this._declaration = declaration;
        this._conflictingDeclaration = conflictingDeclaration;
    }

    [Memo]
    public T Declaration => this.Resolve( this._declaration );

    [Memo]
    public IDeclaration ConflictingDeclaration => this.Resolve( this._conflictingDeclaration );

    ScopedDiagnosticSink IAdviser.Diagnostics => this.AdviceFactory.Diagnostics;

    public T Target => this.Declaration;

    ICompilation IAdviser.Compilation => this.AdviceFactory.Compilation;

    ICompilation IAdviser.MutableCompilation => this.AdviceFactory.MutableCompilation;

    IDeclaration IAdviser.Target => this.Target;

    public IAdviser<TNewDeclaration> With<TNewDeclaration>( TNewDeclaration declaration )
        where TNewDeclaration : class, IDeclaration
        => this.AdviceFactory.WithDeclaration( declaration );

    IAdviceFactory IAdviserInternal.AdviceFactory => this.AdviceFactory;
}