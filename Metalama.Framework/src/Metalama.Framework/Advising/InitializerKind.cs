// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.ComponentModel;

namespace Metalama.Framework.Advising
{
    /// <summary>
    /// Specifies the kind of initializer that should be added by an advice operation.
    /// </summary>
    /// <remarks>
    /// This enumeration is used with methods like <see cref="AdviserExtensions.AddInitializer"/> to determine
    /// when and where initialization code should be injected into a type's constructors or initialization sequence.
    /// </remarks>
    /// <seealso cref="AdviserExtensions"/>
    /// <seealso cref="IAddInitializerAdviceResult"/>
    /// <seealso href="@initializers"/>
    [CompileTime]
    public enum InitializerKind
    {
        /// <summary>
        /// Indicates that the advice should be executed before any user code in all instance constructors except those that are chained to a constructor of the current class (using the <c>this</c> chaining keyword). The initialization logic executes
        /// after the call to the base constructor.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this when you need to initialize instance state or register instances before any user constructor code runs.
        /// The initialization code runs after <c>: base(...)</c> calls but before any user code in the constructor body.
        /// </para>
        /// <para>
        /// A default constructor will be automatically created if the type does not contain any constructor.
        /// </para>
        /// </remarks>
        /// <seealso href="@initializers"/>
        BeforeInstanceConstructor,

        /// <summary>
        /// Indicates that the advice should be executed before the type constructor (aka static constructor) of the target type. If there is no type constructor, this advice adds one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this when you need to initialize static/type-level state before any user static constructor code runs.
        /// The type constructor is called once when the type is first accessed.
        /// </para>
        /// <para>
        /// The initialization code runs at the beginning of the static constructor, after field initializers.
        /// </para>
        /// </remarks>
        /// <seealso href="@initializers"/>
        BeforeTypeConstructor,

        /// <summary>
        /// Indicates that the advice should be executed after all constructors are finished but before the initialization block.
        /// </summary>
        [Obsolete( "Not implemented", true )]
        [EditorBrowsable( EditorBrowsableState.Never )]
        AfterLastInstanceConstructor,

        /// <summary>
        /// Indicates that the advice should be executed after all constructors are finished and after the initialization block.
        /// </summary>
        [Obsolete( "Not implemented", true )]
        [EditorBrowsable( EditorBrowsableState.Never )]
        AfterObjectInitialization,

        /// <summary>
        /// Indicates that the advice should be executed when the instance of a target class is deserialized.
        /// </summary>
        [Obsolete( "Not implemented", true )]
        [EditorBrowsable( EditorBrowsableState.Never )]
        AfterDeserialize,

        /// <summary>
        /// Indicates that the advice should be executed when the instance of a target class is cloned.
        /// </summary>
        [Obsolete( "Not implemented", true )]
        [EditorBrowsable( EditorBrowsableState.Never )]
        AfterMemberwiseClone,

        /// <summary>
        /// Indicated that the advice should be executed when the the target value type is mutated using the "with" expression.
        /// </summary>
        [Obsolete( "Not implemented", true )]
        [EditorBrowsable( EditorBrowsableState.Never )]
        AfterWith
    }
}