// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Rpc;

namespace Metalama.Framework.DesignTime.CodeFixes;

public interface ICodeFixProviderExtension
{
    Task ProvideCodeFixesAsync( ProjectKey projectKey, ICodeFixContext context );
}