// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a custom attribute applied to a declaration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface combines <see cref="IDeclaration"/> (making attributes full code model citizens with their own identity),
    /// <see cref="IAttributeData"/> (providing access to constructor arguments and named arguments), and
    /// <see cref="IAspectPredecessor"/> (enabling attributes to serve as aspect predecessors when they represent aspect custom attributes).
    /// </para>
    /// <para>
    /// To query attributes on a declaration, use the <see cref="IDeclaration.Attributes"/> property, which returns
    /// an <see cref="Collections.IAttributeCollection"/>. To add new attributes to existing declarations, use
    /// <see cref="AdviserExtensions.IntroduceAttribute"/>. To add attributes to introduced declarations, use
    /// <see cref="DeclarationBuilders.IDeclarationBuilder.AddAttribute"/>. To remove attributes, use
    /// <see cref="AdviserExtensions.RemoveAttributes(IAdviser{IDeclaration}, INamedType)">AdviserExtensions.RemoveAttributes</see>. Note that attributes cannot be edited;
    /// to modify an attribute, remove it and add a new one.
    /// </para>
    /// </remarks>
    /// <seealso cref="AttributeExtensions"/>
    /// <seealso cref="IAttributeData"/>
    /// <seealso cref="IDeclaration"/>
    /// <seealso cref="Collections.IAttributeCollection"/>
    /// <seealso cref="DeclarationBuilders.AttributeConstruction"/>
    /// <seealso cref="DeclarationBuilders.IDeclarationBuilder.AddAttribute"/>
    /// <seealso cref="AdviserExtensions.IntroduceAttribute"/>
    /// <seealso cref="AdviserExtensions.RemoveAttributes(IAdviser{IDeclaration}, INamedType)"/>
    /// <seealso href="@adding-attributes"/>
    public interface IAttribute : IDeclaration, IAttributeData, IAspectPredecessor
    {
        /// <summary>
        /// Gets the declaration that owns the custom attribute.
        /// </summary>
        new IDeclaration ContainingDeclaration { get; }

        /// <inheritdoc cref="IDeclaration.ToRef"/>
        new IRef<IAttribute> ToRef();
    }
}