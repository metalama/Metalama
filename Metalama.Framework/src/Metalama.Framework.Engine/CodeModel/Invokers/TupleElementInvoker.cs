// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Invokers;

namespace Metalama.Framework.Engine.CodeModel.Invokers;

internal class TupleElementInvoker : FieldOrPropertyInvoker
{
    public TupleElementInvoker( IFieldOrProperty tupleElement, InvokerOptions options = default, IExpression? target = null ) : base(
        tupleElement,
        options,
        target ) { }

    internal override string GetCleanTargetMemberName() => this.Member.Name;

    protected override IFieldOrPropertyInvoker CreateInvoker( IFieldOrProperty fieldOrProperty, InvokerOptions options, IExpression? target )
        => new TupleElementInvoker( fieldOrProperty, options, target );
}