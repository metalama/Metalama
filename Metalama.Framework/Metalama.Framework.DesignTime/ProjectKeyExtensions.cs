// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.DesignTime;

internal static class ProjectKeyExtensions
{
    public static ProjectKey GetProjectKey( this Compilation compilation ) => ProjectKeyFactory.FromCompilation( compilation );
}