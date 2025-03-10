// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;
using System;

namespace Metalama.Framework.Engine.AdviceImpl.Attributes;

internal sealed class AddAttributeAdviceResult : AdviceResult, IIntroductionAdviceResult<IAttribute>
{
    private readonly IRef<IAttribute>? _attribute;

    public AddAttributeAdviceResult()
    {
        this.AdviceKind = AdviceKind.IntroduceAttribute;
    }

    public AddAttributeAdviceResult( AdviceOutcome outcome, IRef<IAttribute> attribute )
    {
        this.AdviceKind = AdviceKind.IntroduceAttribute;
        this.Outcome = outcome;
        this._attribute = attribute;
    }

    [Memo]
    public IAttribute Declaration => this.Resolve( this._attribute );

    public IDeclaration ConflictingDeclaration => throw new NotSupportedException();

    ScopedDiagnosticSink IAdviser.Diagnostics => throw new NotSupportedException();

    IAttribute IAdviser<IAttribute>.Target => throw new NotSupportedException();

    IDeclaration IAdviser.Target => throw new NotSupportedException();

    IAdviser<TNewDeclaration> IAdviser.With<TNewDeclaration>( TNewDeclaration declaration ) => throw new NotSupportedException();
}