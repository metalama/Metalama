// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Transformations;

internal interface ISyntaxTreeTransformation : ITransformation, ISyntaxTreeTransformationBase;

public interface ISyntaxTreeTransformationBase : ITransformationBase
{
    SyntaxTree TransformedSyntaxTree { get; }
}