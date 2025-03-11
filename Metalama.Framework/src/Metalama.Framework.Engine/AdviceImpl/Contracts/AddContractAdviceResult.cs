// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.Engine.AdviceImpl.Contracts;

internal sealed class AddContractAdviceResult<T> : AdviceResult, IAddContractAdviceResult<T>
    where T : class, IDeclaration
{
    private readonly IRef<T>? _declaration;

    public AddContractAdviceResult() { }

    public AddContractAdviceResult( IRef<T>? declaration )
    {
        this._declaration = declaration;
        this.AdviceKind = AdviceKind.AddContract;
    }

    [Memo]
    public T Declaration => this.Resolve( this._declaration );

    public static AddContractAdviceResult<T> Ignored { get; } = new() { Outcome = AdviceOutcome.Ignore, AdviceKind = AdviceKind.AddContract };
}