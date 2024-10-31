// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal abstract class SourceMember : SourceMemberOrNamedType, IMember
    {
        protected SourceMember( CompilationModel compilation, GenericContext? genericContextForSymbolMapping ) : base(
            compilation,
            genericContextForSymbolMapping ) { }

        public abstract bool IsExplicitInterfaceImplementation { get; }

        public new INamedType DeclaringType => base.DeclaringType.AssertNotNull();

        IMember IMember.Definition => (IMember) this.GetDefinitionMemberOrNamedType();

        IRef<IMember> IMember.ToRef() => this.ToMemberRef();

        public abstract IMember? OverriddenMember { get; }

        protected abstract IRef<IMember> ToMemberRef();

        public abstract bool IsAsync { get; }

        public bool IsVirtual => this.Symbol.IsVirtual;

        public bool IsOverride => this.Symbol.IsOverride;

        public bool HasImplementation
            => this.Symbol switch
            {
                IMethodSymbol { IsPartialDefinition: true, PartialImplementationPart: null } => false,
#if ROSLYN_4_12_0_OR_GREATER
                IPropertySymbol { IsPartialDefinition: true, PartialImplementationPart: null } => false,
#endif
                IFieldSymbol { IsConst: true } => false,
                { IsAbstract: true } => false,
                { IsExtern: true } => false,
                _ => true
            };

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
        {
            if ( !this.CanBeInherited )
            {
                return [];
            }
            else
            {
                return GetDerivedDeclarationsCore( this, options );
            }
        }

        internal static IEnumerable<IDeclaration> GetDerivedDeclarationsCore( IMember self, DerivedTypesOptions options )
        {
            foreach ( var derivedType in self.Compilation.GetDerivedTypes( self.DeclaringType, options ) )
            {
                foreach ( var member in ((INamedTypeImpl) derivedType).GetOverridingMembers( self ) )
                {
                    yield return member;
                }
            }
        }

        public override bool CanBeInherited
            => this.DeclaringType.TypeKind == TypeKind.Interface
               || ((this.IsAbstract || (this.IsVirtual && !this.IsSealed)) && ((IDeclarationImpl) this.DeclaringType).CanBeInherited);
    }
}