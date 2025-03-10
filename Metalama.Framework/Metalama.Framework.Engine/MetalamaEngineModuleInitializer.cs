// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis.Simplification;

namespace Metalama.Framework.Engine;

[PublicAPI]
public static class MetalamaEngineModuleInitializer
{
    static MetalamaEngineModuleInitializer()
    {
        TypeOfResolver.TypeIdResolver = UserCodeExecutionContext.ResolveCompileTimeTypeOf;
        TypeOfResolver.DeclarationIdResolver = UserCodeExecutionContext.ResolveCompileTimeTypeOf;
        FormattingAnnotations.Initialize( Simplifier.Annotation );
        MetalamaStringFormatter.Initialize( new MetalamaStringFormatterImpl() );
    }

    public static void EnsureInitialized() { }
}