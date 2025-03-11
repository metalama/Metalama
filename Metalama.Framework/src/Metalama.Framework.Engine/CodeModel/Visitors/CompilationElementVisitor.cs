// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using System;

namespace Metalama.Framework.Engine.CodeModel.Visitors;

[PublicAPI]
internal abstract class CompilationElementVisitor
{
    protected int RecursionDepth { get; private set; }

    public virtual void Visit( ICompilationElement element )
    {
        if ( this.RecursionDepth > 100 )
        {
            throw new InvalidOperationException( "Excessive recursion." );
        }

        this.RecursionDepth++;

        switch ( element )
        {
            case null:
                // This can happen when an exception is displayed when the object is incompletely constructed.
                this.VisitNull();

                break;

            case IType type:
                switch ( type.TypeKind )
                {
                    case TypeKind.Array:
                        this.VisitArrayType( (IArrayType) type );

                        break;

                    case TypeKind.Class or TypeKind.RecordClass or TypeKind.Struct or TypeKind.RecordStruct or TypeKind.Delegate or TypeKind.Enum
                        or TypeKind.Interface:
                        this.VisitNamedType( (INamedType) type );

                        break;

                    case TypeKind.Dynamic:
                        this.VisitDynamicType( (IDynamicType) type );

                        break;

                    case TypeKind.TypeParameter:
                        this.VisitTypeParameter( (ITypeParameter) type );

                        break;

                    case TypeKind.Pointer:
                        this.VisitPointerType( (IPointerType) type );

                        break;

                    case TypeKind.FunctionPointer:
                        this.VisitFunctionPointerType( (IFunctionPointerType) type );

                        break;

                    case TypeKind.Error:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;

            case IDeclaration declaration:
                switch ( declaration.DeclarationKind )
                {
                    case DeclarationKind.Compilation:
                        this.VisitCompilation( (ICompilation) declaration );

                        break;

                    case DeclarationKind.NamedType:
                        this.VisitNamedType( (INamedType) declaration );

                        break;

                    case DeclarationKind.Method or DeclarationKind.Finalizer or DeclarationKind.Operator:
                        this.VisitMethod( (IMethod) declaration );

                        break;

                    case DeclarationKind.Property:
                        this.VisitProperty( (IProperty) declaration );

                        break;

                    case DeclarationKind.Indexer:
                        this.VisitIndexer( (IIndexer) declaration );

                        break;

                    case DeclarationKind.Field:
                        this.VisitField( (IField) declaration );

                        break;

                    case DeclarationKind.Event:
                        this.VisitEvent( (IEvent) declaration );

                        break;

                    case DeclarationKind.Parameter:
                        this.VisitParameter( (IParameter) declaration );

                        break;

                    case DeclarationKind.TypeParameter:
                        this.VisitTypeParameter( (ITypeParameter) declaration );

                        break;

                    case DeclarationKind.Attribute:
                        this.VisitAttribute( (IAttribute) declaration );

                        break;

                    case DeclarationKind.ManagedResource:
                        this.VisitManagedResource( (IManagedResource) declaration );

                        break;

                    case DeclarationKind.Constructor:
                        this.VisitConstructor( (IConstructor) declaration );

                        break;

                    case DeclarationKind.AssemblyReference:
                        this.VisitAssemblyReference( (IAssembly) declaration );

                        break;

                    case DeclarationKind.Namespace:
                        this.VisitNamespace( (INamespace) declaration );

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(element) );
        }

        this.RecursionDepth--;
    }

    protected abstract void DefaultVisit( ICompilationElement element );

    public virtual void VisitNamespace( INamespace declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitAssemblyReference( IAssembly declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitConstructor( IConstructor declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitManagedResource( IManagedResource declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitAttribute( IAttribute declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitParameter( IParameter declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitIndexer( IIndexer declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitEvent( IEvent declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitField( IField declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitProperty( IProperty declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitMethod( IMethod declaration ) => this.DefaultVisit( declaration );

    public virtual void VisitCompilation( ICompilation declaration ) => this.DefaultVisit( declaration );

    protected virtual void VisitArrayType( IArrayType arrayType ) => this.DefaultVisit( arrayType );

    protected virtual void VisitDynamicType( IDynamicType dynamicType ) => this.DefaultVisit( dynamicType );

    protected virtual void VisitNamedType( INamedType namedType ) => this.DefaultVisit( namedType );

    protected virtual void VisitPointerType( IPointerType pointerType ) => this.DefaultVisit( pointerType );

    protected virtual void VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => this.DefaultVisit( functionPointerType );

    protected virtual void VisitTypeParameter( ITypeParameter typeParameter ) => this.DefaultVisit( typeParameter );

    protected virtual void VisitNull() { }
}