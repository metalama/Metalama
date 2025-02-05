// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed class IntroduceInterfaceTransformation : BaseSyntaxTreeTransformation, IIntroduceInterfaceTransformation, IInjectInterfaceTransformation
{
    public IFullRef<INamedType> InterfaceType { get; }

    public IFullRef<INamedType> TargetType { get; }

    public IReadOnlyDictionary<IMember, IMember> MemberMap { get; }

    public IntroduceInterfaceTransformation(
        AspectLayerInstance implementInterfaceAspectLayerInstance,
        IFullRef<INamedType> targetType,
        IFullRef<INamedType> interfaceType,
        Dictionary<IMember, IMember> memberMap ) : base( implementInterfaceAspectLayerInstance, targetType )
    {
        this.TargetType = targetType;
        this.InterfaceType = interfaceType;
        this.MemberMap = memberMap;
    }

    public BaseTypeSyntax GetSyntax( SyntaxGenerationContext context )
    {
        // The type already implements the interface members itself.
        return SimpleBaseType( context.SyntaxGenerator.TypeSyntax( this.InterfaceType ) );
    }

    public override IFullRef<IDeclaration> TargetDeclaration => this.TargetType;

    public override TransformationObservability Observability => TransformationObservability.Always;

    public override IntrospectionTransformationKind TransformationKind => IntrospectionTransformationKind.ImplementInterface;

    public override FormattableString ToDisplayString() => $"Make the type '{this.TargetType}' implement the interface '{this.InterfaceType}'.";
}