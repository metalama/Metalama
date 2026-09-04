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

    internal static readonly DiagnosticDefinition<(ISymbol Member, ISymbol RecordType, ISymbol Property)>
        SynthesizedRecordMemberReadsPropertyVirtually = new(
            "LAMA0652",
            "The original implementation of a compiler-synthesized record member reads an overridable property.",
            "The original implementation of '{0}' generated for record '{1}' reads the property '{2}', whereas the C# compiler reads its "
            + "backing field. The backing field of an auto-property cannot be read from source code. The two implementations differ when a "
            + "derived type overrides the property. Declare the property explicitly with a backing field, or make it non-overridable.",
            _category,
            Warning );

    internal static readonly DiagnosticDefinition<(ISymbol Member, ISymbol RecordType, ISymbol Property)>
        SynthesizedRecordMemberReadsReplacedProperty = new(
            "LAMA0653",
            "The original implementation of a compiler-synthesized record member reads a property whose implementation an aspect has replaced.",
            "The original implementation of '{0}' generated for record '{1}' reads the property '{2}', whereas the C# compiler reads its "
            + "backing field. An aspect overrides the property with a template that does not call the original implementation, so the "
            + "property has no backing field left and the generated body reads the value that the aspect returns instead of the value "
            + "that the record stores. Call 'meta.Proceed()' in the template that overrides the property.",
            _category,
            Warning );

    internal static readonly DiagnosticDefinition<(ISymbol Member, ISymbol RecordType, ISymbol Property)>
        SynthesizedRecordMemberReadsSemiAutoProperty = new(
            "LAMA0654",
            "The original implementation of a compiler-synthesized record member reads a property whose getter has a body.",
            "The original implementation of '{0}' generated for record '{1}' reads the property '{2}', whereas the C# compiler reads its "
            + "backing field. The property is declared with the 'field' keyword and its getter has a body, so the getter can return a value "
            + "other than the one that the backing field holds, and the two implementations then differ. Declare the property explicitly "
            + "with a backing field, or give it an automatic getter.",
            _category,
            Warning );

    internal static readonly DiagnosticDefinition<(ISymbol Member, ISymbol RecordType, ISymbol Event)>
        SynthesizedRecordMemberReadsReplacedEvent = new(
            "LAMA0655",
            "The original implementation of a compiler-synthesized record member reads a field-like event whose implementation an aspect has replaced.",
            "The original implementation of '{0}' generated for record '{1}' reads the backing field of the field-like event '{2}', which "
            + "the event no longer has. An aspect overrides the event with templates that do not call the original implementation, so the "
            + "linker replaces the event by one that stores no handler and the generated body cannot be compiled. Call 'meta.Proceed()' in "
            + "the templates that override the event.",
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