// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Transformations
{
    internal abstract class AspectReferenceSyntaxProvider
    {
        // TODO: Expressions for other kinds of aspect references should be created through this class (SoC).

        public const string LinkerOverrideParamName = "__linker_param";

        public abstract ExpressionSyntax GetPropertyReference(
            AspectLayerId aspectLayer,
            IProperty overriddenProperty,
            AspectReferenceTargetKind targetKind,
            ContextualSyntaxGenerator syntaxGenerator );

        public abstract ExpressionSyntax GetIndexerReference(
            AspectLayerId aspectLayer,
            IIndexer overriddenProperty,
            AspectReferenceTargetKind targetKind,
            ContextualSyntaxGenerator syntaxGenerator );

        public abstract ExpressionSyntax GetFinalizerReference( AspectLayerId aspectLayer );

        public abstract ExpressionSyntax GetStaticConstructorReference( AspectLayerId aspectLayer );

        public abstract ExpressionSyntax GetConstructorReference(
            AspectLayerId aspectLayer,
            IConstructor overriddenConstructor,
            ContextualSyntaxGenerator syntaxGenerator );

        public abstract ExpressionSyntax GetOperatorReference( AspectLayerId aspectLayer, IMethod targetOperator, ContextualSyntaxGenerator syntaxGenerator );

        public abstract ExpressionSyntax GetEventFieldInitializerExpression( TypeSyntax eventFieldType, ExpressionSyntax initializerExpression );
    }
}