// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Introspection;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.LinkerTests.Runner;

/// <summary>
/// A base class for test transformations that inject processed syntax instead of running templates.
/// </summary>
internal abstract class TestTransformationBase : IInjectMemberTransformation
{
    public InsertPosition InsertPosition { get; }

    public AspectLayerInstance AspectLayerInstance { get; }

    public AspectLayerId AspectLayerId => this.AspectLayerInstance.AspectLayerId;

    public IAspectInstanceInternal AspectInstance => this.AspectLayerInstance.AspectInstance;

    public int OrderWithinPipeline { get; set; }

    public int OrderWithinPipelineStepAndType { get; set; }

    public int OrderWithinPipelineStepAndTypeAndAspectInstance { get; set; }

    protected TestTransformationBase( AspectLayerInstance aspectLayerInstance, InsertPosition insertPosition )
    {
        this.AspectLayerInstance = aspectLayerInstance;
        this.InsertPosition = insertPosition;
    }

    public abstract TransformationObservability Observability { get; }

    public abstract IRef<IDeclaration> TargetDeclaration { get; }

    public abstract SyntaxTree TransformedSyntaxTree { get; }

    public IAspectClass AspectClass => throw new NotSupportedException();

    public IntrospectionTransformationKind TransformationKind => throw new NotSupportedException();

    public abstract IEnumerable<InjectedMember> GetInjectedMembers( MemberInjectionContext context );

    public FormattableString ToDisplayString() => $"Test";
}