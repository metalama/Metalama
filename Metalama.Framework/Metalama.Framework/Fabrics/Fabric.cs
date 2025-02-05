// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Serialization;

namespace Metalama.Framework.Fabrics
{
    /// <summary>
    /// Allows adding aspects or analyzing a project, namespace, or type just by adding a type inheriting this class.
    /// You cannot inherit this class directly, inherit from <see cref="ProjectFabric"/>, <see cref="NamespaceFabric"/>,
    /// or <see cref="TypeFabric"/> instead.
    /// </summary>
    /// <seealso href="@fabrics-adding-aspects"/>
    [CompileTime]
    public abstract class Fabric : ICompileTimeSerializable, ITemplateProvider
    {
        private protected Fabric() { }
    }
}