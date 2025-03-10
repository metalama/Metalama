// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Visitors;
using System;

namespace Metalama.Framework.Engine.CodeModel.GenericContexts;

public partial class GenericContext
{
    internal static bool ReferencesAnyIntroducedType( IType type ) => IntroducedTypeDetector.Instance.Visit( type );

    private sealed class IntroducedTypeDetector : TypeVisitor<bool>
    {
        private IntroducedTypeDetector() { }

        public static IntroducedTypeDetector Instance { get; } = new();

        protected override bool DefaultVisit( IType type ) => throw new NotImplementedException( $"Type kind not handled: {type.TypeKind}." );

        protected override bool VisitArrayType( IArrayType arrayType ) => this.Visit( arrayType.ElementType );

        protected override bool VisitDynamicType( IDynamicType dynamicType ) => false;

        protected override bool VisitNamedType( INamedType namedType ) => namedType is IntroducedNamedType;

        protected override bool VisitPointerType( IPointerType pointerType ) => this.Visit( pointerType.PointedAtType );

        protected override bool VisitFunctionPointerType( IFunctionPointerType functionPointerType ) => false;

        protected override bool VisitTypeParameter( ITypeParameter typeParameter ) => false;
    }
}