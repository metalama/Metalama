// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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