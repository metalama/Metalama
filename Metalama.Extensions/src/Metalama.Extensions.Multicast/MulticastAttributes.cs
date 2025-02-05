// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.Multicast;

/// <summary>
///   Attributes (i.e. in C# terms, modifiers) of elements to which multicast custom attributes (<see cref = "IMulticastAttribute" />)
///   apply.
/// </summary>
/// <remarks>
/// <para>
/// There are 8 categories of flags. If you specify at least one flag for a category, you narrow the multicast just to elements
/// that have one of the flags you specified in the category. For example, if you specify <c>Public | Protected</c>, you multicast
/// to elements that are public or protected, but not private. Their other attributes (such as whether they are instance or static) do not matter. 
/// </para>
/// <para>
/// The categories are visibility (public, private, ...); scope (static or instance); abstraction (abstract or non-abstract); virtuality (virtual
/// or non-virtual); implementation (managed or unmanaged); literality (literal or non-literal); generation (compiler-generated or user-created); and
/// parameter (in, ref, or out).
/// </para>
/// <para>
/// If a category doesn't make sense for an element, then specifying its flags will have no effect for that element. For example, if you narrow
/// your multicast down to virtual elements, it will still apply to fields or types (which can't be virtual). 
/// </para>
/// </remarks>
[Flags]
[RunTimeOrCompileTime]
[PublicAPI]
public enum MulticastAttributes
{
    /// <summary>
    ///   Specifies that the set of target attributes is inherited from
    ///   the parent custom attribute.
    /// </summary>
    Default = 0,

    /// <summary>
    ///   Private (visible inside the current type).
    /// </summary>
    Private = 1 << 1,

    /// <summary>
    ///   Protected (visible inside derived types).
    /// </summary>
    Protected = 1 << 2,

    /// <summary>
    ///   Internal (visible inside the current assembly).
    /// </summary>
    Internal = 1 << 3,

    /// <summary>
    ///   Internal <i>and</i> protected (visible inside derived types that are defined in the current assembly).
    /// </summary>
    InternalAndProtected = 1 << 4,

    /// <summary>
    ///   Internal <i>or</i> protected (visible inside all derived types and in the current assembly).
    /// </summary>
    InternalOrProtected = 1 << 5,

    /// <summary>
    ///   Public (visible everywhere).
    /// </summary>
    Public = 1 << 6,

    /// <summary>
    ///   Any visibility.
    /// </summary>
    AnyVisibility = Private | Protected | Internal | InternalAndProtected | InternalOrProtected | Public,

    /// <summary>
    ///   Static scope.
    /// </summary>
    Static = 1 << 7,

    /// <summary>
    ///   Instance scope.
    /// </summary>
    Instance = 1 << 8,

    /// <summary>
    ///   Any scope (<see cref = "Static" /> | <see cref = "Instance" />).
    /// </summary>
    AnyScope = Static | Instance,

    /// <summary>
    ///   Abstract methods.
    /// </summary>
    Abstract = 1 << 9,

    /// <summary>
    ///   Concrete (non-abstract) methods.
    /// </summary>
    NonAbstract = 1 << 10,

    /// <summary>
    ///   Any abstraction (<see cref = "Abstract" /> | <see cref = "NonAbstract" />).
    /// </summary>
    AnyAbstraction = Abstract | NonAbstract,

    /// <summary>
    ///   Virtual methods.
    /// </summary>
    Virtual = 1 << 11,

    /// <summary>
    ///   Non-virtual methods.
    /// </summary>
    NonVirtual = 1 << 12,

    /// <summary>
    ///   Any virtuality (<see cref = "Virtual" /> | <see cref = "NonVirtual" />).
    /// </summary>
    AnyVirtuality = NonVirtual | Virtual,

    /// <summary>
    ///   Managed code implementation.
    /// </summary>
    Managed = 1 << 13,

    /// <summary>
    ///   Non-managed code implementation (external or system).
    /// </summary>
    NonManaged = 1 << 14,

    /// <summary>
    ///   Any implementation (<see cref = "Managed" /> | <see cref = "NonManaged" />).
    /// </summary>
    AnyImplementation = Managed | NonManaged,

    /// <summary>
    ///   Literal fields.
    /// </summary>
    Literal = 1 << 15,

    /// <summary>
    ///   Non-literal fields.
    /// </summary>
    NonLiteral = 1 << 16,

    /// <summary>
    ///   Any field literality (<see cref = "Literal" /> | <see cref = "NonLiteral" />).
    /// </summary>
    AnyLiterality = Literal | NonLiteral,

    /// <summary>
    ///   Input parameters.
    /// </summary>
    InParameter = 1 << 17,

    /// <summary>
    ///   Compiler-generated code.
    /// </summary>
    CompilerGenerated = 1 << 18,

    /// <summary>
    ///   User-generated code (anything expected <see cref = "CompilerGenerated" />).
    /// </summary>
    UserGenerated = 1 << 19,

    /// <summary>
    ///   Any code generation (<see cref = "CompilerGenerated" /> | <see cref = "UserGenerated" />).
    /// </summary>
    AnyGeneration = CompilerGenerated | UserGenerated,

    /// <summary>
    ///   Output (<b>out</b> in C#) parameters.
    /// </summary>
    OutParameter = 1 << 20,

    /// <summary>
    ///   Input/Output (<b>ref</b> in C#) parameters.
    /// </summary>
    RefParameter = 1 << 21,

    /// <summary>
    ///   Any kind of parameter passing (<see cref = "InParameter" /> | <see cref = "OutParameter" /> | <see cref = "RefParameter" />).
    /// </summary>
    AnyParameter = InParameter | OutParameter | RefParameter,

    /// <summary>
    ///   All members.
    /// </summary>
    All =
        AnyVisibility | AnyVirtuality | AnyScope | AnyImplementation | AnyLiterality | AnyAbstraction |
        AnyGeneration | AnyParameter
}