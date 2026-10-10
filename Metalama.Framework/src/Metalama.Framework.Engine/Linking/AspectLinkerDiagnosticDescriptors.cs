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
            "Cannot invoke the base implementation of a member on an instance other than 'this'.",
            "Cannot invoke the base implementation of '{0}' on an instance other than 'this', because C# allows base calls only on the current " +
            "instance. Use InvokerOptions.Final to invoke the member on another instance.",
            _category,
            Error );

    internal static readonly DiagnosticDefinition<ISymbol>
        CannotUseProceedWithSynthesizedRecordMember = new(
            "LAMA0651",
            "Cannot use meta.Proceed() with a compiler-synthesized record member.",
            "Cannot use meta.Proceed() when overriding the compiler-synthesized record member '{0}'. Remove the call to meta.Proceed() from the template.",
            _category,
            Error );

    internal static readonly DiagnosticDefinition<(string AspectType, ISymbol TargetDeclaration)>
        DeclarationMustBeInlined = new(
            "LAMA0699",
            "The implementation of a constructor or indexer cannot be inlined.",
            "The implementation of '{1}' provided by '{0}' cannot be inlined, but Metalama can only generate inlined code for constructors and " +
            "indexers. Call meta.Proceed() at most once in the template, in a simple statement such as 'meta.Proceed();' or 'return " +
            "meta.Proceed();'.",
            _category,
            Error );
}