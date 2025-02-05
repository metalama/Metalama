// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Code.DeclarationBuilders
{
    public interface IConstructorBuilder : IConstructor, IMethodBaseBuilder
    {
        new ConstructorInitializerKind InitializerKind { get; set; }

        void AddInitializerArgument( IExpression initializerArgumentExpression, string? parameterName = null );
    }
}