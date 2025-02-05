// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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