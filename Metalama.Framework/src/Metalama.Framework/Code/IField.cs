// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a field. Note that fields can be promoted to properties by aspects.
    /// </summary>
    /// <seealso cref="IFieldOrProperty"/>
    /// <seealso cref="IProperty"/>
    /// <seealso cref="FieldKind"/>
    /// <seealso cref="FieldAspect"/>
    /// <seealso href="@overriding-fields-or-properties"/>
    public interface IField : IFieldOrProperty
    {
        /// <summary>
        /// Converts the current compile-time field to a run-time <see cref="FieldInfo"/> object.
        /// </summary>
        /// <seealso href="@reflection"/>
        [CompileTimeReturningRunTime]
        FieldInfo ToFieldInfo();

        /// <summary>
        /// Gets the value of the field, if the field is a <c>const</c>. Not to be confused with the <see cref="IFieldOrProperty.InitializerExpression"/>,
        /// which is available even if the field is not <c>const</c>, but only when the field is defined in source code (as opposed to being defined
        /// in a referenced assembly).
        /// </summary>
        TypedConstant? ConstantValue { get; }

        /// <summary>
        /// Gets the definition of the field. If the current declaration is a field of
        /// a generic type instance, this returns the field in the generic type definition. Otherwise, it returns the current instance.
        /// </summary>
        new IField Definition { get; }

        /// <summary>
        /// Creates a reference to this field.
        /// </summary>
        new IRef<IField> ToRef();

        /// <summary>
        /// Gets the property that this field has been overridden into. The opposite side of this relationship is the <see cref="IProperty.OriginalField"/>
        /// of the <see cref="IProperty"/> interface.
        /// </summary>
        IProperty? OverridingProperty { get; }

        /// <summary>
        /// Gets the kind of field.
        /// </summary>
        FieldKind FieldKind { get; }
    }
}