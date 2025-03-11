// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using System;

namespace Metalama.Framework.Engine.CodeModel.Visitors;

[PublicAPI]
internal abstract class CompilationElementVisitor<T>
{
    public virtual T? Visit( ICompilationElement element )
        => element switch
        {
            IType type => type.TypeKind switch
            {
                TypeKind.Array => this.VisitArrayType( (IArrayType) type ),
                TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct or TypeKind.Delegate or TypeKind.Enum or TypeKind.Interface
                    => this.VisitNamedType( (INamedType) type ),
                TypeKind.Dynamic => this.VisitDynamicType( (IDynamicType) type ),
                TypeKind.TypeParameter => this.VisitTypeParameter( (ITypeParameter) type ),
                TypeKind.Pointer => this.VisitPointerType( (IPointerType) type ),
                TypeKind.FunctionPointer => this.VisitFunctionPointerType( (IFunctionPointerType) type ),
                TypeKind.Error => default,
                _ => throw new ArgumentOutOfRangeException()
            },

            IDeclaration declaration => declaration.DeclarationKind switch
            {
                DeclarationKind.Compilation => this.VisitCompilation( (ICompilation) declaration ),
                DeclarationKind.NamedType => this.VisitNamedType( (INamedType) declaration ),
                DeclarationKind.Method or DeclarationKind.Finalizer or DeclarationKind.Operator => this.VisitMethod( (IMethod) declaration ),
                DeclarationKind.Property => this.VisitProperty( (IProperty) declaration ),
                DeclarationKind.Indexer => this.VisitIndexer( (IIndexer) declaration ),
                DeclarationKind.Field => this.VisitField( (IField) declaration ),
                DeclarationKind.Event => this.VisitEvent( (IEvent) declaration ),
                DeclarationKind.Parameter => this.VisitParameter( (IParameter) declaration ),
                DeclarationKind.TypeParameter => this.VisitTypeParameter( (ITypeParameter) declaration ),
                DeclarationKind.Attribute => this.VisitAttribute( (IAttribute) declaration ),
                DeclarationKind.ManagedResource => this.VisitManagedResource( (IManagedResource) declaration ),
                DeclarationKind.Constructor => this.VisitConstructor( (IConstructor) declaration ),
                DeclarationKind.AssemblyReference => this.VisitAssemblyReference( (IAssembly) declaration ),
                DeclarationKind.Namespace => this.VisitNamespace( (INamespace) declaration ),
                _ => throw new ArgumentOutOfRangeException()
            },
            _ => throw new AssertionFailedException()
        };

    protected abstract T DefaultVisit( ICompilationElement element );

    public virtual T VisitNamespace( INamespace declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitAssemblyReference( IAssembly declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitConstructor( IConstructor declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitManagedResource( IManagedResource declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitAttribute( IAttribute declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitParameter( IParameter declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitIndexer( IIndexer declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitEvent( IEvent declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitField( IField declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitProperty( IProperty declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitMethod( IMethod declaration ) => this.DefaultVisit( declaration );

    public virtual T VisitCompilation( ICompilation declaration ) => this.DefaultVisit( declaration );

    protected virtual T VisitArrayType( IArrayType arrayType ) => this.DefaultVisit( arrayType );

    protected virtual T VisitDynamicType( IDynamicType dynamicType ) => this.DefaultVisit( dynamicType );

    protected virtual T VisitNamedType( INamedType namedType ) => this.DefaultVisit( namedType );

    protected virtual T VisitPointerType( IPointerType pointerType ) => this.DefaultVisit( pointerType );

    protected virtual T VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => this.DefaultVisit( functionPointerType );

    protected virtual T VisitTypeParameter( ITypeParameter typeParameter ) => this.DefaultVisit( typeParameter );
}