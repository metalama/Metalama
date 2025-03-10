// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Collections;

internal abstract class DeclarationCollection<TDeclaration> : DeclarationCollection<TDeclaration, IFullRef<TDeclaration>>
    where TDeclaration : class, IDeclaration
{
    protected DeclarationCollection( IDeclaration containingDeclaration, IReadOnlyList<IFullRef<TDeclaration>> source ) : base(
        containingDeclaration,
        source ) { }

    protected DeclarationCollection() { }
}