// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable CommentTypo

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Read-only list of <see cref="IProperty"/>.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    public interface IPropertyCollection : IMemberCollection<IProperty>
    {
        /// <summary>
        /// Gets a property of a given name or throws an exception if there is none.
        /// </summary>
        IProperty this[ string name ] { get; }

        // TODO: Implement.
        ///// <summary>
        ///// Gets a list of properties or indexers with signatures compatible with specified constraints.
        ///// </summary>
        ///// <param name="name">Name of the property or &quot;Items&quot; in case of indexers.</param>
        ///// <param name="argumentTypes">Constraint on reflection types of arguments of an indexer. <c>Null</c>items in the list signify any type.</param>
        ///// <param name="isStatic">Constraint on staticity of the property.</param>
        ///// <param name="declaredOnly"><c>True</c> if only declared property or indexer should be considered or <c>false</c> if all properties and indexers, including those declared in base types should be considered.</param>
        ///// <returns>Enumeration of properties and indexers matching specified constraints. If <paramref name="declaredOnly" /> is set to <c>false</c>, only the top-most visible property of the same signature is included.</returns>
        // IEnumerable<IProperty> OfCompatibleSignature(
        //    string name,
        //    IReadOnlyList<Type?>? argumentTypes,
        //    bool? isStatic = false,
        //    bool declaredOnly = true );

        ///// <summary>
        ///// Gets a list of properties or indexers with signatures compatible with specified constraints.
        ///// </summary>
        ///// <param name="name">Name of the property or &quot;Items&quot; in case of indexers.</param>
        ///// <param name="argumentTypes">Constraint on types of arguments of an indexer. <c>Null</c>items in the list signify any type.</param>
        ///// <param name="refKinds">Constraint on reference kinds of arguments of an indexer. <c>Null</c>items in the list signify any reference kind.</param>
        ///// <param name="isStatic">Constraint on staticity of the property.</param>
        ///// <param name="declaredOnly"><c>True</c> if only declared property or indexer should be considered or <c>false</c> if all properties and indexers, including those declared in base types should be considered.</param>
        ///// <returns>Enumeration of properties and indexers matching specified constraints. If <paramref name="declaredOnly" /> is set to <c>false</c>, only the top-most visible property of the same signature is included.</returns>
        // IEnumerable<IProperty> OfCompatibleSignature(
        //    string name,
        //    IReadOnlyList<IType?>? argumentTypes = null,
        //    IReadOnlyList<RefKind?>? refKinds = null,
        //    bool? isStatic = false,
        //    bool declaredOnly = true );

        ///// <summary>
        ///// Gets a property or an indexer that exactly matches the specified signature.
        ///// </summary>
        ///// <param name="name">Name of the property or &quot;Items&quot; in case of indexers.</param>
        ///// <param name="parameterTypes">List of parameter types for indexers or <c>null</c> in case of properties.</param>
        ///// <param name="refKinds">List of parameter reference kinds, or <c>null</c> if all parameters should be by-value.</param>
        ///// <param name="isStatic">Staticity of the property.</param>
        ///// <param name="declaredOnly"><c>True</c> if only declared methods should be considered or <c>false</c> if all methods, including those declared in base types should be considered.</param>
        ///// <returns>A <see cref="IProperty"/> that matches the given signature. If <paramref name="declaredOnly" /> is set to <c>false</c>, the top-most visible property or indexer is shown.</returns>
        // IProperty? OfExactSignature(
        //    string name,
        //    IReadOnlyList<IType>? parameterTypes = null,
        //    IReadOnlyList<RefKind>? refKinds = null,
        //    bool? isStatic = null,
        //    bool declaredOnly = true );
    }
}