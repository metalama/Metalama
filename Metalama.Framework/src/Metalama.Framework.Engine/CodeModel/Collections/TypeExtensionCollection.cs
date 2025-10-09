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

internal class TypeExtensionCollection : MemberOrNamedTypeCollection<ITypeExtension>, ITypeExtensionCollection
{
    public TypeExtensionCollection( INamedType declaringType, IReadOnlyList<IFullRef<ITypeExtension>> sourceItems ) : base(
        declaringType,
        sourceItems ) { }

    public IEnumerable<ITypeExtension> ForType( IType type ) => this.GetItems( this.Source ).Where( t => t.ExtendedType.Equals( type ) );

    public IEnumerable<ITypeExtension> ForType( Type type ) => this.ForType( this.Compilation.Factory.GetTypeByReflectionType( type ) );
}