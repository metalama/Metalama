// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Engine.Transformations;

internal interface ITemplateLexicalScopeProvider
{
    TemplateLexicalScope GetLexicalScope( IFullRef<IDeclaration> declaration );
}