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
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class AddAttributeAdviceResult : AdviceResult, IIntroductionAdviceResult<IAttribute>
{
    private readonly IRef<IAttribute>? _attribute;
    private readonly IAdviceFactory _adviceFactory;

    public AddAttributeAdviceResult(
        AdviceOutcome outcome,
        IAdviceFactoryImpl adviceFactory,
        IRef<IAttribute>? attribute = null,
        ImmutableArray<Diagnostic> diagnostics = default )
        : base( AdviceKind.IntroduceAttribute, outcome, adviceFactory, diagnostics )
    {
        this._attribute = attribute;
        this._adviceFactory = adviceFactory;
    }

    [Memo]
    public IAttribute Declaration => this.Resolve( this._attribute );

    public IDeclaration ConflictingDeclaration => throw new NotSupportedException();

    ScopedDiagnosticSink IAdviser.Diagnostics => throw new NotSupportedException();

    IAttribute IAdviser<IAttribute>.Target => throw new NotSupportedException();

    ICompilation IAdviser.Compilation => this._adviceFactory.Compilation;

    ICompilation IAdviser.MutableCompilation => this._adviceFactory.MutableCompilation;

    IDeclaration IAdviser.Target => throw new NotSupportedException();

    IAdviser<TNewDeclaration> IAdviser.With<TNewDeclaration>( TNewDeclaration declaration ) => throw new NotSupportedException();
}