// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

[CompileTime]
public interface IAssemblyCollection : IReadOnlyCollection<IAssembly>
{
    IEnumerable<IAssembly> OfName( string name );
}