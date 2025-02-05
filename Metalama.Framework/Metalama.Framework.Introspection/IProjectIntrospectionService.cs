// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;

namespace Metalama.Framework.Introspection;

public interface IProjectIntrospectionService : IProjectService
{
    IIntrospectionReferenceGraph GetReferenceGraph( ICompilation compilation );
}