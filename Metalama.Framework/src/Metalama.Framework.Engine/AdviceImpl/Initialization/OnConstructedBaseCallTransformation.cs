// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

/// <summary>
/// Aggregatable transformation that emits the <c>base.OnConstructed(context.Descend(slotA | slotB | ...))</c>
/// prologue statement at the top of an introduced <c>OnConstructed</c> override. See
/// <see cref="BaseInitializationCallTransformation"/> for details on aggregation behavior.
/// </summary>
internal sealed class OnConstructedBaseCallTransformation : BaseInitializationCallTransformation
{
    private static readonly object _aggregateKey = new();

    protected override string MethodName => "OnConstructed";

    public override object AggregateKey => _aggregateKey;

    public OnConstructedBaseCallTransformation(
        AspectLayerInstance aspectLayerInstance,
        IRef<IMemberOrNamedType> contextDeclaration,
        IFullRef<IMethod> targetMethod,
        string contextParameterName,
        IReadOnlyList<IField>? slotFields )
        : base( aspectLayerInstance, contextDeclaration, targetMethod, contextParameterName, slotFields ) { }
}