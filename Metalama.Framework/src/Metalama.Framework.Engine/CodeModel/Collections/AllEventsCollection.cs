// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal sealed class AllEventsCollection : AllMembersCollection<IEvent>, IEventCollection
{
    public AllEventsCollection( INamedTypeImpl declaringType ) : base( declaringType ) { }

    protected override IMemberCollection<IEvent> GetMembers( INamedType namedType ) => namedType.Events;

    protected override IEqualityComparer<IEvent> Comparer => this.CompilationContext.EventComparer;
}