// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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