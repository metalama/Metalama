// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.Collections;
using System.Collections.Immutable;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represent the members of a custom attribute, but not its relationship to the containing declaration.
    /// </summary>
    /// <remarks>
    /// Values of <see cref="ConstructorArguments"/> and <see cref="NamedArguments"/> are represented as:
    /// <list type="bullet">
    /// <item>Primitive types as themselves (e.g. int as int, string as string).</item>
    /// <item>Enums as their underlying type.</item>
    /// <item><see cref="System.Type"/> as <see cref="IType"/>.</item>
    /// <item>Arrays as <c>IReadOnlyList&lt;object&gt;</c>.</item>
    /// </list>
    /// </remarks>
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