// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Describes the kind of constructor initializer. A constructor initializer is the optional
    /// <c>: base(...)</c> or <c>: this(...)</c> clause that appears after the constructor parameter list
    /// and invokes another constructor before the current constructor body executes.
    /// </summary>
    /// <remarks>
    /// <para>For example, in <c>public MyClass() : base(42) { }</c>, the <c>: base(42)</c> part is a constructor initializer
    /// of kind <see cref="Base"/>.</para>
    /// <para>In <c>public MyClass(int x) : this() { }</c>, the <c>: this()</c> part is a constructor initializer
    /// of kind <see cref="This"/>.</para>
    /// </remarks>
    /// <seealso cref="IConstructor"/>
    /// <seealso cref="IConstructorBuilder"/>
    [CompileTime]
    public enum ConstructorInitializerKind
    {
        /// <summary>
        /// The constructor has no initializer calling a base or sibling constructor. This applies to value types (structs)
        /// and static constructors, which never have a <c>base</c> or <c>this</c> initializer. For instance constructors of reference types (classes),
        /// the C# compiler always implicitly calls a base constructor, so <see cref="Base"/> is returned instead.
        /// </summary>
        None,

        /// <summary>
        /// The constructor invokes a constructor of the base class, either explicitly via a <c>: base(...)</c> initializer
        /// or implicitly when the instance constructor has no explicit initializer and the declaring type is a reference type.
        /// </summary>
        Base,

        /// <summary>
        /// The constructor has a <c>: this(...)</c> initializer that invokes another constructor of the same type.
        /// </summary>
        This
    }
}