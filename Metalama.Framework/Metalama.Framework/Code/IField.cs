// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Reflection;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a field. Note that fields can be promoted to properties by aspects.
    /// </summary>
    public interface IField : IFieldOrProperty
    {
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

        new IRef<IField> ToRef();

        /// <summary>
        /// Gets the property that this field has been overridden into. The opposite side of this relationship is the <see cref="IProperty.OriginalField"/>
        /// of the <see cref="IProperty"/> interface.
        /// </summary>
        IProperty? OverridingProperty { get; }
    }
}