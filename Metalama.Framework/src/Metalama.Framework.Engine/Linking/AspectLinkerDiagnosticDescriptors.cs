// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using static Metalama.Framework.Diagnostics.Severity;

namespace Metalama.Framework.Engine.Linking;

public static class AspectLinkerDiagnosticDescriptors
{
    // Reserved range 650-699

    private const string _category = "Metalama.Linker";

    internal static readonly DiagnosticDefinition<ISymbol>
        CannotInvokeAnotherInstanceBaseRequired = new(
            "LAMA0650",
            "Can't invoke member, because correct invocation would require a base call on an instance other than this.",
            "Can't invoke member '{0}', because correct invocation would require a base call on an instance other than this.",
            _category,
            Error );

    internal static readonly DiagnosticDefinition<(string AspectType, ISymbol TargetDeclaration)>
        DeclarationMustBeInlined = new(
            "LAMA0699",
            "Declaration must be inlined.",
            "Version of declaration '{1} provided by '{0}' cannot be inlined. It is not currently possible to generate non-inlined code for this declaration.",
            _category,
            Error );
}