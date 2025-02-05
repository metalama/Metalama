// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Read-only list of <see cref="INamespace"/>.
    /// </summary>
    /// <remarks>
    ///  <para>The order of items in this list is undetermined and may change between versions.</para>
    /// </remarks>
    [InternalImplement]
    public interface INamespaceCollection : INamedDeclarationCollection<INamespace>
    {
        new INamespace? OfName( string name );
    }
}