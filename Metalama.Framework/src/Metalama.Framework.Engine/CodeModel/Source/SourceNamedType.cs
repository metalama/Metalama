// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    /// <summary>
    /// The public object that represents an <see cref="INamedType"/>. The implementation is in <see cref="SourceNamedTypeImpl"/>.
    /// This class exists because it needs to add a dependency context check before each member access, which makes
    /// it hard to use [Memo].
    /// </summary>
    internal class SourceNamedType : SourceMemberOrNamedType, INamedTypeImpl
    {
        public INamedTypeSymbol NamedTypeSymbol { get; }

        public SourceNamedTypeImpl Implementation { get; }

        internal SourceNamedType(
            INamedTypeSymbol typeSymbol,
            CompilationModel compilation,
            GenericContext? genericContextForSymbolMapping ) : this(
            typeSymbol,
            compilation,
            genericContextForSymbolMapping,
            new SourceNamedTypeImpl( typeSymbol, compilation, genericContextForSymbolMapping ) ) { }

        protected SourceNamedType(
            INamedTypeSymbol typeSymbol,
            CompilationModel compilation,
            GenericContext? genericContextForSymbolMapping,
            SourceNamedTypeImpl implementation ) : base(
            compilation,
            genericContextForSymbolMapping )
        {
            this.NamedTypeSymbol = typeSymbol;
            this.Implementation = implementation;
            implementation.Facade = this;
        }

        protected override void OnUsingDeclaration() => UserCodeExecutionContext.CurrentOrNull?.AddDependencyFrom( this );

        public override bool CanBeInherited
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.CanBeInherited;
            }
        }

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        {
            this.OnUsingDeclaration();

            return this.Implementation.GetDerivedDeclarations( options );
        }

        public override DeclarationKind DeclarationKind
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.DeclarationKind;
            }
        }

        public override ISymbol Symbol => this.NamedTypeSymbol;

        public override MemberInfo ToMemberInfo()
        {
            this.OnUsingDeclaration();

            return this.Implementation.ToMemberInfo();
        }

        public TypeKind TypeKind
        {
            get
            {
                this.OnUsingDeclaration();

                return ((IType) this.Implementation).TypeKind;
            }
        }

        public SpecialType SpecialType
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.SpecialType;
            }
        }

        public Type ToType()
        {
            this.OnUsingDeclaration();

            return this.Implementation.ToType();
        }

        public bool? IsReferenceType
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsReferenceType;
            }
        }

        public bool? IsNullable
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsNullable;
            }
        }

        public bool Equals( SpecialType specialType )
        {
            this.OnUsingDeclaration();

            return this.Implementation.Equals( specialType );
        }

        public bool Equals( IType? otherType, TypeComparison typeComparison ) => this.Implementation.Equals( otherType, typeComparison );

        public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
            => this.Implementation.Equals( otherType, typeComparison );

        public override bool Equals( object? obj )
            => obj switch
            {
                IType otherType => this.Equals( otherType ),
                Type otherType => this.Equals( otherType ),
                _ => false
            };

        public IArrayType MakeArrayType( int rank = 1 ) => this.Compilation.Factory.MakeArrayType( this.NamedTypeSymbol, rank );

        public IPointerType MakePointerType() => this.Compilation.Factory.MakePointerType( this.NamedTypeSymbol );

        public INamedType ToNullable() => (INamedType) this.Compilation.Factory.MakeNullableType( this, true );

        public IType ToNonNullable() => this.Compilation.Factory.MakeNullableType( this, false );

        public INamedType StripNullabilityAnnotation() => (INamedType) this.Compilation.Factory.MakeNullableType( this, null );

        IType IType.StripNullabilityAnnotation() => this.StripNullabilityAnnotation();

        public INamedType MakeGenericInstance( IReadOnlyList<IType> typeArguments )
        {
            this.OnUsingDeclaration();

            return this.Implementation.MakeGenericInstance( typeArguments );
        }

        IType IType.ToNullable() => this.ToNullable();

        public ITypeParameterList TypeParameters
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.TypeParameters;
            }
        }

        public IReadOnlyList<IType> TypeArguments
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.TypeArguments;
            }
        }

        public bool IsGeneric
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsGeneric;
            }
        }

        public bool IsCanonicalGenericInstance
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsCanonicalGenericInstance;
            }
        }

        public override bool IsPartial
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsPartial;
            }
        }

        public bool HasDefaultConstructor
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.HasDefaultConstructor;
            }
        }

        public INamedType? BaseType
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.BaseType;
            }
        }

        public IImplementedInterfaceCollection AllImplementedInterfaces
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllImplementedInterfaces;
            }
        }

        public IImplementedInterfaceCollection ImplementedInterfaces
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.ImplementedInterfaces;
            }
        }

        INamespace INamedType.Namespace => this.ContainingNamespace;

        public INamespace ContainingNamespace
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.ContainingNamespace;
            }
        }

        public IRef<INamedType> ToRef() => this.Implementation.Ref;

        IRef<IType> IType.ToRef() => this.Implementation.Ref;

        IRef<INamespaceOrNamedType> INamespaceOrNamedType.ToRef() => this.Implementation.Ref;

        INamedTypeCollection INamedType.NestedTypes => this.Types;

        public string FullName
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.FullName;
            }
        }

        public INamedTypeCollection Types
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Types;
            }
        }

        public INamedTypeCollection AllTypes
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllTypes;
            }
        }

        public IPropertyCollection Properties
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Properties;
            }
        }

        public IPropertyCollection AllProperties
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllProperties;
            }
        }

        public IIndexerCollection Indexers
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Indexers;
            }
        }

        public IIndexerCollection AllIndexers
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllIndexers;
            }
        }

        public IFieldCollection Fields
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Fields;
            }
        }

        public IFieldCollection AllFields
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllFields;
            }
        }

        public IFieldOrPropertyCollection FieldsAndProperties
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.FieldsAndProperties;
            }
        }

        public IFieldOrPropertyCollection AllFieldsAndProperties
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllFieldsAndProperties;
            }
        }

        public IEventCollection Events
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Events;
            }
        }

        public IEventCollection AllEvents
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllEvents;
            }
        }

        public IMethodCollection Methods
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Methods;
            }
        }

        public IMethodCollection AllMethods
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.AllMethods;
            }
        }

        public IConstructorCollection Constructors
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Constructors;
            }
        }

        public IConstructor? PrimaryConstructor
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.PrimaryConstructor;
            }
        }

        public IConstructor? StaticConstructor
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.StaticConstructor;
            }
        }

        public IMethod? Finalizer
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.Finalizer;
            }
        }

        public IExtensionBlockCollection ExtensionBlocks
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.ExtensionBlocks;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsReadOnly;
            }
        }

        public bool IsRef
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsRef;
            }
        }

        public bool IsRecord
        {
            get
            {
                this.OnUsingDeclaration();

                return this.Implementation.IsRecord;
            }
        }

        ICompilation ICompilationElement.Compilation => this.Compilation;

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Implementation.Ref;

        public bool IsSubclassOf( INamedType type )
        {
            this.OnUsingDeclaration();

            return this.Implementation.IsSubclassOf( type );
        }

        public bool TryFindImplementationForInterfaceMember( IMember interfaceMember, [NotNullWhen( true )] out IMember? implementationMember )
        {
            this.OnUsingDeclaration();

            return this.Implementation.TryFindImplementationForInterfaceMember( interfaceMember, out implementationMember );
        }

        [Memo]
        public INamedType Definition
            => this.NamedTypeSymbol.Equals( this.NamedTypeSymbol.OriginalDefinition )
                ? this
                : this.Compilation.Factory.GetNamedType( this.NamedTypeSymbol.OriginalDefinition );

        protected override IRef<IMemberOrNamedType> ToMemberOrNamedTypeRef() => this.UnderlyingType.ToRef();

        protected override IMemberOrNamedType GetDefinitionMemberOrNamedType() => this.Definition;

        INamedType INamedType.TypeDefinition => this.Definition;

        public INamedType UnderlyingType => this.Implementation.UnderlyingType;

        public IType Accept( TypeRewriter visitor ) => visitor.Visit( this );

        public IReadOnlyList<IMember> GetOverridingMembers( IMember member )
        {
            this.OnUsingDeclaration();

            return this.Implementation.GetOverridingMembers( member );
        }

        public bool IsImplementationOfInterfaceMember( IMember typeMember, IMember interfaceMember )
        {
            this.OnUsingDeclaration();

            return this.Implementation.IsImplementationOfInterfaceMember( typeMember, interfaceMember );
        }

        internal ITypeImpl WithTypeArguments( IReadOnlyList<IType> types )
        {
            var hasDifference = false;

            for ( var i = 0; i < types.Count; i++ )
            {
                if ( !ReferenceEquals( types[i], this.TypeArguments[i] ) )
                {
                    hasDifference = true;

                    break;
                }
            }

            if ( !hasDifference )
            {
                return this;
            }

            var typeArgumentSymbols = new ITypeSymbol[types.Count];

            for ( var index = 0; index < types.Count; index++ )
            {
                var t = types[index];
                typeArgumentSymbols[index] = t.GetSymbol().AssertSymbolNotNull();
            }

            var symbol = this.NamedTypeSymbol.ConstructedFrom.Construct( typeArgumentSymbols );

            return (ITypeImpl) this.Compilation.Factory.GetIType( symbol, defaultNullability: null );
        }

        public override IDeclaration ContainingDeclaration => this.Implementation.ContainingDeclaration;

        public bool Equals( IType? other )
        {
            this.OnUsingDeclaration();

            return this.Implementation.Equals( other );
        }

        public bool Equals( INamedType? other )
        {
            this.OnUsingDeclaration();

            return this.Implementation.Equals( other );
        }

        public override int GetHashCode()
        {
            this.OnUsingDeclaration();

            return this.Implementation.GetHashCode();
        }
    }
}