// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.DesignTime.CodeFixes;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.VisualStudio.CodeFixes;

[UsedImplicitly]
[PublicAPI]
public sealed class VsCodeRefactoringProvider : TheCodeRefactoringProvider
{
    public VsCodeRefactoringProvider( ServiceProvider<IGlobalService> serviceProvider ) : base( serviceProvider ) { }

    public VsCodeRefactoringProvider() { }
}