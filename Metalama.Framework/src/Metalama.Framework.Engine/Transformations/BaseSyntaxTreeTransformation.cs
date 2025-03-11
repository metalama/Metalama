// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Transformations;

internal abstract class BaseSyntaxTreeTransformation : BaseTransformation, ISyntaxTreeTransformation
{
    protected BaseSyntaxTreeTransformation( AspectLayerInstance aspectLayerInstance, SyntaxTree transformedSyntaxTree ) : base( aspectLayerInstance )
    {
        this.TransformedSyntaxTree = transformedSyntaxTree;
    }

    protected BaseSyntaxTreeTransformation( AspectLayerInstance aspectLayerInstance, IFullRef<IDeclaration> targetDeclaration ) : base( aspectLayerInstance )
    {
        this.TransformedSyntaxTree = targetDeclaration.PrimarySyntaxTree
                                     ?? aspectLayerInstance.InitialCompilation.PartialCompilation.SyntaxTreeForCompilationLevelAttributes;
    }

    public SyntaxTree TransformedSyntaxTree { get; }
}