// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;

namespace Metalama.Framework.Engine.CodeModel.Visitors;

/// <summary>
/// Visitor for <see cref="IType"/>.
/// </summary>
internal abstract class TypeVisitor<T>
{
    public T Visit( IType type )
        => type switch
        {
            IArrayType arrayType => this.VisitArrayType( arrayType ),
            IDynamicType dynamicType => this.VisitDynamicType( dynamicType ),
            INamedType namedType => this.VisitNamedType( namedType ),
            IPointerType pointerType => this.VisitPointerType( pointerType ),
            IFunctionPointerType functionPointerType => this.VisitFunctionPointerType( functionPointerType ),
            ITypeParameter typeParameter => this.VisitTypeParameter( typeParameter ),
            IFunctionPointerType or _ => throw new AssertionFailedException( $"Unexpected type: {type.GetType()}" ),
        };

    // ReSharper disable once UnusedParameter.Global

    protected abstract T DefaultVisit( IType type );

    protected virtual T VisitArrayType( IArrayType arrayType ) => this.DefaultVisit( arrayType );

    protected virtual T VisitDynamicType( IDynamicType dynamicType ) => this.DefaultVisit( dynamicType );

    protected virtual T VisitNamedType( INamedType namedType ) => this.DefaultVisit( namedType );

    protected virtual T VisitPointerType( IPointerType pointerType ) => this.DefaultVisit( pointerType );

    protected virtual T VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => this.DefaultVisit( functionPointerType );

    protected virtual T VisitTypeParameter( ITypeParameter typeParameter ) => this.DefaultVisit( typeParameter );
}