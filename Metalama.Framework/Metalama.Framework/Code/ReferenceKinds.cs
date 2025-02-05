// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Enumerates all kinds of references.
    /// </summary>
    /// <seealso href="@validation"/>
    [CompileTime]
    [Flags]
    public enum ReferenceKinds : long
    {
        None = 0,

        /// <summary>
        /// All reference kinds.
        /// </summary>
        All = -1,

        /// <summary>
        /// A field or property access that does not fall into another category.
        /// </summary>
        Default = 1 << 0,

        /// <summary>
        /// Base type or interface.
        /// </summary>
        BaseType = 1 << 1,

        /// <summary>
        /// Type argument (e.g. generic argument). 
        /// </summary>
        TypeArgument = 1 << 2,

        /// <summary>
        /// <c>typeof</c>.
        /// </summary>
        TypeOf = 1 << 3,

        /// <summary>
        /// Parameter reference.
        /// </summary>
        ParameterType = 1 << 4,

        /// <summary>
        /// Type constraint of a generic parameter.
        /// </summary>
        TypeConstraint = 1 << 5,

        /// <summary>
        /// Object construction, i.e. constructor invocation. The reference generally points to the constructor. In case of collection expression, the reference
        /// points to the type.
        /// </summary>
        ObjectCreation = 1 << 7,

        /// <summary>
        /// Type of a field, property, or event.
        /// </summary>
        MemberType = 1 << 8,

        /// <summary>
        /// Type of a local variable.
        /// </summary>
        LocalVariableType = 1 << 9,

        /// <summary>
        /// Type of a custom attribute.
        /// </summary>
        AttributeType = 1 << 10,

        /// <summary>
        /// Return type of a method.
        /// </summary>
        ReturnType = 1 << 11,

        /// <summary>
        /// Element type of an array.
        /// </summary>
        ArrayElementType = 1 << 12,

        [Obsolete( "Renamed to ArrayElementType." )]
        ArrayType = ArrayElementType,

        /// <summary>
        /// Nullable type.
        /// </summary>
        [Obsolete( "No longer detected." )]
        NullableType = 1 << 13,

        /// <summary>
        /// Type of element pointed at by an unmanaged pointer.
        /// </summary>
        PointerType = 1 << 14,

        /// <summary>
        /// Type of a <c>ref</c>.
        /// </summary>
        [Obsolete( "No longer detected." )]
        RefType = 1 << 15,

        [Obsolete( "Renamed to TypleElementType." )]
        TupleType = TupleElementType,

        /// <summary>
        /// Type of a tuple element.
        /// </summary>
        TupleElementType = 1 << 16,

        /// <summary>
        /// Invocation of a method or delegate.
        /// </summary>
        Invocation = 1 << 17,

        /// <summary>
        /// Left part of an assignment.
        /// </summary>
        Assignment = 1 << 18,

        /// <summary>
        /// The member is being overridden using an <c>override</c>. The reference points to the overridden member.
        /// </summary>
        OverrideMember = 1 << 19,

        /// <summary>
        /// Implicit or explicit implementation of an interface member. The reference points to the interface member.
        /// </summary>
        InterfaceMemberImplementation = 1 << 20,

        /// <summary>
        /// Inside a <c>using</c> namespace directive.
        /// </summary>
        UsingNamespace = 1 << 21,

        [Obsolete( "Renamed to UsingNamespace." )]
        Using = UsingNamespace,

        /// <summary>
        /// <c>nameof</c>.
        /// </summary>
        NameOf = 1 << 22,

        /// <summary>
        /// Base constructor (either <c>this</c> or <c>base</c>). The reference points to the base constructor.
        /// </summary>
        BaseConstructor = 1 << 23,

        /// <summary>
        /// Creation of an array. The reference points to the type.
        /// </summary>
        ArrayCreation = 1 << 24,

        /// <summary>
        /// Type of a cast (both <c>(T) x</c> and <c>x as T</c>).
        /// </summary>
        CastType = 1 << 25,

        /// <summary>
        /// <c>is T</c> or <c>is T {}</c> pattern matching.
        /// </summary>
        IsType = 1 << 26
    }
}