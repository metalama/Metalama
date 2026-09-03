// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace TemplateLanguageVersion14;

/// <summary>
/// An aspect that introduces a property whose accessors use the <c>field</c> keyword of C# 14.
/// </summary>
/// <remarks>
/// The accessors of an introduced property are templates, so the template compiler verifies them against
/// <c>MetalamaTemplateLanguageVersion</c>. The <c>field</c> keyword is represented by a syntax node that Roslyn 5.0
/// added, so the verification reports LAMA0232 when that property is lower than 14.0.
/// </remarks>
public class TestAspect : TypeAspect
{
    [Introduce]
    public string Message
    {
        get => field;
        set => field = value;
    }
}

// <target>
[TestAspect]
internal class Target { }
