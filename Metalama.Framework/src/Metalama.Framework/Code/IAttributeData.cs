// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using System.Collections.Immutable;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents the data of a custom attribute (type, constructor arguments, and named arguments) without its relationship to the containing declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides access to the intrinsic data of a custom attribute. It is implemented by both <see cref="IAttribute"/>
    /// (for attributes in the code model) and <see cref="DeclarationBuilders.AttributeConstruction"/> (for creating new attributes programmatically).
    /// </para>
    /// <para>
    /// Values of <see cref="ConstructorArguments"/> and <see cref="NamedArguments"/> are represented as:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Primitive types as themselves (e.g., <c>int</c> as <c>int</c>, <c>string</c> as <c>string</c>).</description></item>
    /// <item><description>Enums as their underlying type.</description></item>
    /// <item><description><see cref="System.Type"/> as <see cref="IType"/>.</description></item>
    /// <item><description>Arrays as <c>IReadOnlyList&lt;object&gt;</c>.</description></item>
    /// </list>
    /// <para>
    /// To introduce an attribute to a declaration using this interface, pass an <see cref="IAttributeData"/> instance
    /// to <see cref="AdviserExtensions.IntroduceAttribute"/>. Use <see cref="DeclarationBuilders.AttributeConstruction.Create(IConstructor, System.Collections.Generic.IReadOnlyList{TypedConstant}?, System.Collections.Generic.IReadOnlyList{System.Collections.Generic.KeyValuePair{string, TypedConstant}}?)"/>
    /// to create a new <see cref="IAttributeData"/> programmatically.
    /// </para>
    /// </remarks>
    /// <seealso cref="IAttribute"/>
    /// <seealso cref="TypedConstant"/>
    /// <seealso cref="INamedArgumentList"/>
    /// <seealso cref="DeclarationBuilders.AttributeConstruction"/>
    /// <seealso cref="AdviserExtensions.IntroduceAttribute"/>
    /// <seealso href="@adding-attributes"/>
    [CompileTime]
    public interface IAttributeData
    {
        /// <summary>
        /// Gets the custom attribute type.
        /// </summary>
        INamedType Type { get; }

        /// <summary>
        /// Gets the constructor to be used to instantiate the custom attribute.
        /// </summary>
        IConstructor Constructor { get; }

        /// <summary>
        /// Gets the parameters passed to the <see cref="Constructor"/>.
        /// </summary>
        ImmutableArray<TypedConstant> ConstructorArguments { get; }

        /// <summary>
        /// Gets the named arguments (either fields or properties) of the attribute.
        /// </summary>
        INamedArgumentList NamedArguments { get; }
    }
}