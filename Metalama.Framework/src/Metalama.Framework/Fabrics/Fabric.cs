// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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