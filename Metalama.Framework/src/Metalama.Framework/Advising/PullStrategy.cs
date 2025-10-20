// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising.PullStrategies;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

namespace Metalama.Framework.Advising;

public static class PullStrategy
{
    public static IPullStrategy UseExpression( IExpression expression ) => new ExpressionPullStrategy( expression );

    public static IPullStrategy UseConstant( TypedConstant constant ) => new ExpressionPullStrategy( constant );

    public static IPullStrategy IntroduceParameterAndPull(
        string? name = null,
        IType? type = null,
        IExpression? defaultValue = null )
        => new IntroduceParameterPullStrategy( name, type?.ToRef(), defaultValue?.ToText() );
}