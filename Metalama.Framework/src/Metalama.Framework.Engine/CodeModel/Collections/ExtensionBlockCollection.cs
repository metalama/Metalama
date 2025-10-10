// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Engine.CodeModel.References;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal class ExtensionBlockCollection : MemberOrNamedTypeCollection<IExtensionBlock>, IExtensionBlockCollection
{
    public ExtensionBlockCollection( INamedType declaringType, IReadOnlyList<IFullRef<IExtensionBlock>> sourceItems ) : base(
        declaringType,
        sourceItems ) { }

    public IEnumerable<IExtensionBlock> OfReceivingType( IType type ) => this.GetItems( this.Source ).Where( t => t.ReceiverType.Equals( type ) );

    public IEnumerable<IExtensionBlock> OfReceivingType( Type type ) => this.OfReceivingType( this.Compilation.Factory.GetTypeByReflectionType( type ) );
}