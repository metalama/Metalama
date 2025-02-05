// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.CodeModel;

internal sealed record CompilationModelOptions( bool ShowExternalPrivateMembers = false )
{
    public static readonly CompilationModelOptions Default = new();
}