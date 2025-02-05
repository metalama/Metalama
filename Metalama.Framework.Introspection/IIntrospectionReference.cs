// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Code;

namespace Metalama.Framework.Introspection;

public interface IIntrospectionReference
{
    [PublicAPI]
    IDeclaration DestinationDeclaration { get; }

    IDeclaration OriginDeclaration { get; }

    [PublicAPI]
    ReferenceKinds Kinds { get; }

    IReadOnlyList<IntrospectionReferenceDetail> Details { get; }
}