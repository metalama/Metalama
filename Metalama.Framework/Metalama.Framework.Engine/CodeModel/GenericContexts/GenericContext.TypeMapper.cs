// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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