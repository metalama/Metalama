// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class AddContractAdviceResult<T> : AdviceResult, IAddContractAdviceResult<T>
    where T : class, IDeclaration
{
    private readonly IRef<T>? _declaration;

    public AddContractAdviceResult(
        AdviceOutcome outcome,
        IAdviceFactoryImpl adviceFactory,
        IRef<T>? declaration = null,
        ImmutableArray<Diagnostic> diagnostics = default ) : base( AdviceKind.AddContract, outcome, adviceFactory, diagnostics )
    {
        this._declaration = declaration;
    }

    [Memo]
    public T Declaration => this.Resolve( this._declaration );

    public static AddContractAdviceResult<T> Ignored( IAdviceFactoryImpl adviceFactory ) => new( AdviceOutcome.Ignore, adviceFactory );

    // Preserves the target declaration reference so user code reading `result.Declaration` after a
    // pipeline-driven skip continues to see the original target.
    public static AddContractAdviceResult<T> Skipped( IAdviceFactoryImpl adviceFactory, IRef<T>? declaration = null )
        => new( AdviceOutcome.Skipped, adviceFactory, declaration );
}