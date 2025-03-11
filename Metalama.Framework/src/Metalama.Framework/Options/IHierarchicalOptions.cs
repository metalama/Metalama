// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Options;

/// <summary>
/// Base interface for hierarchical options. Hierarchical options are options exposed and consumed by
/// aspects and fabrics. Users can typically set options at different levels of the code level: for the
/// whole project, for a namespace, for a type, or for a member, as decided by the aspect author.
/// </summary>
/// <remarks>
/// <para>
///  Users typically set options from a fabric using the <see cref="OptionQueryExtensions.SetOptions{TDeclaration,TOptions}(Metalama.Framework.Fabrics.IQuery{TDeclaration},System.Func{TDeclaration,TOptions})"/>
/// method of the <see cref="IAmender{T}.Outbound"/> object.
/// </para>
/// <para>
///  Implementations of this class must be immutable. An instance of this object represents a <i>layer</i> of options,
///  and these layers are merged by the <see cref="IIncrementalObject.ApplyChanges"/> method. Therefore, all properties should typically be nullable or
///  support another representation of being unset.
/// </para>
/// <para>
/// Classes that implement this interface must implement the <see cref="IHierarchicalOptions{T}"/> generic interface where <c>T</c> is
/// the type of declarations for which the user is allowed to set the options. Typically, a single class would implement several instances
/// of this instance. For instance, for an option affecting a method-level aspect, a good practice is to implement this interface
/// for <see cref="ICompilation"/>, <see cref="INamespace"/>, <see cref="INamedType"/> and <see cref="IMethod"/>.
/// </para>
/// <para>
/// Classes that implement this interface can be annotated with the <see cref="HierarchicalOptionsAttribute"/> custom attribute,
/// which allows authors to customize the inheritance mechanisms of the option.
/// </para>
/// <para>
/// Attribute classes and aspect classes can implement the <see cref="IHierarchicalOptionsProvider"/> interface if they want to contribute
/// options.
/// </para>
/// <para>
/// Options are exposed by the <c>declaration.</c><see cref="DeclarationExtensions.Enhancements{T}"/>.<see cref="DeclarationEnhancements{T}.GetOptions{TOptions}"/> method.
/// </para> 
/// </remarks>
/// <seealso href="@exposing-options"/>
public interface IHierarchicalOptions : IIncrementalObject, ICompileTimeSerializable
{
    /// <summary>
    /// Gets the default options from the current project. 
    /// </summary>
    /// <returns>The default options for the given project, or <c>null</c> if the default value is the instance initialized by the default constructor.</returns>
    /// <remarks>
    /// <para>
    ///  If the aspect supports parameters supplied as MSBuild project properties, the implementation of this
    ///  method should read these properties and assign their values to the returned object. Otherwise, it can return <c>null</c>.
    /// </para>
    /// </remarks>
    IHierarchicalOptions? GetDefaultOptions( OptionsInitializationContext context )
#if NET5_0_OR_GREATER
        => null;
#else
        ;
#endif
}

/// <summary>
/// An interface, derived from the non-generic <see cref="IHierarchicalOptions"/>, that means that the options can be set
/// on the type of declarations specified by the generic parameter.
/// </summary>
/// <typeparam name="T">The type of declarations on which the options can be set or read.</typeparam>
/// <remarks>
///  See the remarks for the non-generic <see cref="IHierarchicalOptions"/> interface.
/// </remarks>
/// <seealso cref="IHierarchicalOptions"/>
public interface IHierarchicalOptions<in T> : IHierarchicalOptions
    where T : class, IDeclaration;