// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerAnalysisStep
{
    /// <summary>
    /// Represents a call site (object creation or <c>with</c> expression) that targets a type
    /// implementing <c>IInitializable</c> and needs to be rewritten by the Linker.
    /// </summary>
    public readonly struct ObjectCreationCallSiteReference
    {
        /// <summary>
        /// Gets the method body that contains this call site.
        /// </summary>
        public IntermediateSymbolSemantic<IMethodSymbol> ContainingSemantic { get; }

        /// <summary>
        /// Gets the syntax node to be replaced (ObjectCreationExpressionSyntax,
        /// ImplicitObjectCreationExpressionSyntax, or WithExpressionSyntax).
        /// </summary>
        public SyntaxNode ReferencingNode { get; }

        /// <summary>
        /// Gets a value indicating whether this is a <c>with</c> expression
        /// (which requires <see cref="Metalama.Framework.RunTime.Initialization.InitializationMetadata.Modify"/> metadata).
        /// </summary>
        public bool IsWithExpression { get; }

        /// <summary>
        /// Gets the cached type info from the registry.
        /// </summary>
        public InitializableTypeInfo TypeInfo { get; }

        /// <summary>
        /// Gets the resolved constructor symbol, or <c>null</c> for <c>with</c> expressions.
        /// </summary>
        public IMethodSymbol? Constructor { get; }

        /// <summary>
        /// Gets the name of the <c>InitializationContext</c> parameter to supply as a named argument,
        /// or <c>null</c> if no context argument should be appended to the constructor call.
        /// </summary>
        public string? ContextParamName { get; }

        public ObjectCreationCallSiteReference(
            IntermediateSymbolSemantic<IMethodSymbol> containingSemantic,
            SyntaxNode referencingNode,
            bool isWithExpression,
            InitializableTypeInfo typeInfo,
            IMethodSymbol? constructor,
            string? contextParamName )
        {
            this.ContainingSemantic = containingSemantic;
            this.ReferencingNode = referencingNode;
            this.IsWithExpression = isWithExpression;
            this.TypeInfo = typeInfo;
            this.Constructor = constructor;
            this.ContextParamName = contextParamName;
        }
    }
}
