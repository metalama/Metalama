// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Visitors;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    private sealed class TypeMapper : TypeRewriter
    {
        private readonly GenericContext _genericContext;

        public TypeMapper( GenericContext genericContext )
        {
            this._genericContext = genericContext;
        }

        internal override IType Visit( ITypeParameter typeParameter ) => this._genericContext.Map( typeParameter );
    }
}