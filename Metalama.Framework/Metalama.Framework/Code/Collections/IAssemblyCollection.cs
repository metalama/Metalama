// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

[CompileTime]
public interface IAssemblyCollection : IReadOnlyCollection<IAssembly>
{
    IEnumerable<IAssembly> OfName( string name );
}