// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;

namespace Metalama.Extensions.Multicast;

#pragma warning disable SA1623 // Property summary documentation should match accessors

/// <summary>
/// An interface that exposes properties that are equivalent those of the PostSharp <c>MulticastAttribute</c>.
/// Multicast aspects in Metalama should implement this interface. All properties should be implemented as automatic properties. 
/// </summary>
[RunTimeOrCompileTime]
[PublicAPI]
public interface IMulticastAttribute : IAspect
{
    /// <summary>
    /// Gets or sets the kind of elements to which this custom attributes applies.
    /// </summary>
    MulticastTargets AttributeTargetElements { get; set; }

    /// <summary>
    /// Multicasting to external assemblies is not supported in Metalama.
    /// </summary>
    [Obsolete( ObsoleteMessages.ExternalAssemblies, true )]
    string? AttributeTargetAssemblies { get; set; }

    /// <summary>
    /// Gets or sets the expression specifying to which types
    /// this instance applies.
    /// </summary>
    /// <value>
    /// A wildcard or regular expression specifying to which types
    /// this instance applies, or <c>null</c> this instance
    /// applies either to all types. Regular expressions should
    /// start with the <c>regex:</c> prefix.
    /// </value>
    /// <remarks>
    /// <para>Ignored if the <see cref="AttributeTargetElements"/> are only the module and/or the assembly.
    /// </para>
    /// <para>Unless you use a wildcard or a regex, you must specify the fully qualified name of the type.</para>
    /// <para>Nested types are delimited by a plus sign (<c>+</c>) in place of a dot (<c>.</c>).</para>
    /// <para>If the type is generic, add a backtick and its type arity at the end.</para>
    /// <para>Examples:
    /// <list type="bullet">
    ///    <item>Namespace.OuterType`1+NestedType`2</item>
    ///    <item>regex:Namespace.*Nested.*</item>
    /// </list>
    /// </para>
    /// </remarks>
    string? AttributeTargetTypes { get; set; }

    /// <summary>
    /// Gets or sets the attributes of types to which this attribute applies. Visibility, scope (<see cref="MulticastAttributes.Instance"/> or <see cref="MulticastAttributes.Static"/>)
    ///   and generation are the only categories that are taken into account; attributes of other categories are ignored.
    /// </summary>
    MulticastAttributes AttributeTargetTypeAttributes { get; set; }

    /// <summary>
    /// Multicasting to external types is not supported in Metalama.
    /// </summary>
    [Obsolete( ObsoleteMessages.ExternalAssemblies, true )]
    MulticastAttributes AttributeTargetExternalTypeAttributes { get; set; }

    /// <summary>
    /// Gets or sets the expression specifying to which members 
    /// this instance applies.
    /// </summary>
    /// <value>
    /// A wildcard or regular expression specifying to which members
    /// this instance applies, or <c>null</c> this instance
    /// applies either to all members whose kind is given in <see cref="AttributeTargetElements"/>.
    /// Regular expressions should start with the <c>regex:</c> prefix.
    /// </value>
    /// <remarks>
    /// <para>Ignored if the only <see cref="AttributeTargetElements"/> are only types.
    /// </para>
    /// </remarks>
    string? AttributeTargetMembers { get; set; }

    /// <summary>
    /// Gets or sets the visibilities, scopes, virtualities, and other characteristics 
    ///  of members to which this attribute applies.
    /// </summary>
    /// <remarks>
    /// <para>Ignored if the <see cref="AttributeTargetElements"/> are only the module, the assembly,
    /// and/or types.
    /// </para>
    /// <para>
    /// The <see cref="MulticastAttributes"/> enumeration is a multi-part flag: there is one
    /// part for visibility, one for scope, one for virtuality, and so on.
    /// If you specify one part, it will override the values defined on the custom attribute definition.
    /// If you do not specify it, the values defined on the custom attribute definition will be inherited.
    /// Note that custom attributes may apply restrictions on these attributes. For instance, 
    /// a custom attribute may not be valid on abstract methods. You are obviously not allowed
    /// to 'enlarge' the set of possible targets.
    /// </para>
    /// </remarks>
    MulticastAttributes AttributeTargetMemberAttributes { get; set; }

    /// <summary>
    /// Multicasting to external types is not supported in Metalama.
    /// </summary>
    [Obsolete( ObsoleteMessages.ExternalAssemblies, true )]
    MulticastAttributes AttributeTargetExternalMemberAttributes { get; set; }

    /// <summary>
    /// Gets or sets the expression specifying to which parameters 
    /// this instance applies.
    /// </summary>
    /// <value>
    /// A wildcard or regular expression specifying to which parameters
    /// this instance applies, or <c>null</c> this instance
    /// applies either to all members whose kind is given in <see cref="AttributeTargetElements"/>.
    /// Wildcard expressions should
    /// start with the <c>regex:</c> prefix.
    /// </value>
    /// <remarks>
    /// <para>Ignored if the only <see cref="AttributeTargetElements"/> are only types.
    /// </para>
    /// </remarks>
    string? AttributeTargetParameters { get; set; }

    /// <summary>
    /// Gets or sets the passing style (by value, <b>out</b> or <b>ref</b>)
    ///  of parameters to which this attribute applies.
    /// </summary>
    /// <remarks>
    /// <para>Ignored if the <see cref="AttributeTargetElements"/> do not include parameters.
    /// </para>
    /// </remarks>
    MulticastAttributes AttributeTargetParameterAttributes { get; set; }

    /// <summary>
    /// If true, indicates that this attribute <i>removes</i> all other instances of the
    /// same attribute type from the set of elements defined by the current instance.
    /// </summary>
    bool AttributeExclude { get; set; }

    /// <summary>
    /// Gets or sets the priority of the current attribute in case that multiple 
    /// instances are defined on the same element (lower values are processed before).
    /// </summary>
    /// <remarks>
    /// You should use only 16-bit values in user code. Top 16 bits are reserved for the system.
    /// </remarks>
    int AttributePriority { get; set; }

    /// <summary>
    /// This property not supported in Metalama. Metalama always behaves as if this property were <c>true</c>.
    /// </summary>
    [Obsolete( ObsoleteMessages.AttributeReplace, true )]
    bool AttributeReplace { get; set; }

    /// <summary>
    /// Determines whether this attribute is inherited.
    /// </summary>
    /// <remarks>
    /// <para>If this property is not set to <c>MulticastInheritance.None</c>,
    /// a copy of this attribute will be propagated
    /// along the lines of inheritance of the target element:</para>
    /// <list type="bullet">
    /// <item>On <b>classes</b>: all classed derived from that class.</item>
    /// <item>On <b>interfaces</b>: all classes implementing this interface.</item>
    /// <item>On <b>virtual, abstract or interface methods</b>: all methods overriding 
    /// or implementing this method.</item>
    /// <item>On <b>parameters</b> or <b>return value</b> of virtual, abstract or interface methods:
    /// corresponding parameter or return value on all methods or overriding or implementing the
    /// parent method of the target parameter or return value.</item>
    /// </list>
    /// </remarks>
    [Obsolete( ObsoleteMessages.Inheritance, true )]
    MulticastInheritance AttributeInheritance { get; set; }
}