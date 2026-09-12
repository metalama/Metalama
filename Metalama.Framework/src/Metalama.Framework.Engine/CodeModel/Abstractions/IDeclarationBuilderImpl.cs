// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel.Introductions.Collections;

namespace Metalama.Framework.Engine.CodeModel.Abstractions
{
    internal interface IDeclarationBuilderImpl : IDeclarationBuilder, IDeclarationImpl
    {
        AspectLayerInstance AspectLayerInstance { get; }

        new AttributeBuilderCollection Attributes { get; }

        bool IsDesignTimeObservable { get; }

        /// <summary>
        /// Gets a value indicating whether the compiler synthesizes the declaration from the declaration of the type
        /// that contains it, so that nothing emits syntax of its own for it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Section 4.2 of <c>Metalama.Framework/docs/introducing-types.md</c> names those members: the
        /// <c>Invoke</c> method of a delegate, the members that a record declaration implies, the <c>Value</c>
        /// property and the per-case constructors of a union, and the parameterless constructor of a struct. An
        /// advice that needs a declaration of its own, such as one that adds a custom attribute, is refused on such
        /// a member. The property that a positional parameter of a record declares is not one of them, because the
        /// parameter is a declaration on which an attribute can be written.
        /// </para>
        /// </remarks>
        bool IsSynthesizedByCompiler { get; }
    }
}