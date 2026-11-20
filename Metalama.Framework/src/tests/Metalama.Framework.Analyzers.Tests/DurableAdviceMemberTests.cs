// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Xunit;

namespace Metalama.Framework.Analyzers.Tests;

/// <summary>
/// Tests that the members of an aspect that are advice, rather than state, are exempt from the durable contract.
/// </summary>
/// <remarks>
/// <c>IAspect</c> is marked <c>[Durable]</c>, so every aspect is bound by the contract. A member marked with an advice
/// attribute, such as <c>[Introduce]</c> or <c>[IntroduceDependency]</c>, describes code generated into the target and
/// is not held by the aspect instance, so its declared type is irrelevant to what the aspect retains. These members
/// typically have a run-time-only type, such as a logger, and reporting them would make the rule fire on the most
/// common aspects. The immutability contract exempts the same members.
/// </remarks>
public sealed class DurableAdviceMemberTests : DurableAnalyzerTestBase
{
    private const string _preamble = """
                                     using Metalama.Framework.Advising;
                                     using Metalama.Framework.Aspects;
                                     using Metalama.Framework.Code;
                                     using System;
                                     using System.Collections.Concurrent;

                                     public interface ILogger { }

                                     """;

    private static string Code( string body ) => _preamble + body;

    /// <remarks>
    /// The control: the same field without an advice attribute is state of the aspect.
    /// </remarks>
    [Fact]
    public async Task AFieldOfAnAspectWithoutAdviceAttribute_IsReported()
        => await AssertSingleDiagnosticAsync(
            Code( "class LogAttribute : OverrideMethodAspect { private readonly ILogger _logger; public override dynamic? OverrideMethod() => null; }" ),
            "LAMA0876" );

    [Fact]
    public async Task AnIntroducedField_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class LogAttribute : OverrideMethodAspect { [Introduce] private readonly ILogger _logger; public override dynamic? OverrideMethod() => null; }" ) );

    [Fact]
    public async Task AnIntroducedFieldOfAConcreteNonDurableType_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                "class CacheAttribute : OverrideMethodAspect { [Introduce] private readonly ConcurrentDictionary<string, object?> _cache = new(); public override dynamic? OverrideMethod() => null; }" ) );

    [Fact]
    public async Task AnIntroducedAutomaticProperty_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class LogAttribute : TypeAspect { [Introduce] public ILogger? Logger { get; set; } }" ) );

    [Fact]
    public async Task ATemplateAutomaticProperty_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code( "class LogAttribute : TypeAspect { [Template] public ILogger? Logger { get; set; } }" ) );

    /// <remarks>
    /// <c>[IntroduceDependency]</c> is declared in an extension package, which is represented here by a custom advice
    /// attribute. The exemption is keyed on the advice attribute interface, not on a list of attribute types.
    /// </remarks>
    [Fact]
    public async Task AFieldMarkedWithACustomDeclarativeAdviceAttribute_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                """
                class IntroduceDependencyAttribute : DeclarativeAdviceAttribute { }

                class LogAttribute : OverrideMethodAspect
                {
                    [IntroduceDependency]
                    private readonly ILogger _logger;

                    public override dynamic? OverrideMethod() => null;
                }
                """ ) );

    /// <remarks>
    /// An override of a template property is itself a template, as for the template classifier of the engine.
    /// </remarks>
    [Fact]
    public async Task AnOverrideOfATemplateAutomaticProperty_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                """
                abstract class BaseAttribute : TypeAspect { [Template] public virtual ILogger? Logger { get; set; } }

                class LogAttribute : BaseAttribute { public override ILogger? Logger { get; set; } }
                """ ) );

    [Fact]
    public async Task APropertyImplementingAnInterfaceTemplateProperty_IsNotReported()
        => await AssertNoDiagnosticAsync(
            Code(
                """
                [RunTimeOrCompileTime]
                interface IHasLogger { [Template] ILogger? Logger { get; set; } }

                class LogAttribute : TypeAspect, IHasLogger { public ILogger? Logger { get; set; } }
                """ ) );

    /// <remarks>
    /// The exemption applies to the advice member only: the other members of the same aspect are still verified.
    /// </remarks>
    [Fact]
    public async Task AStateFieldNextToAnIntroducedField_IsReported()
    {
        var message = await AssertSingleDiagnosticAsync(
            Code(
                """
                class LogAttribute : OverrideMethodAspect
                {
                    [Introduce]
                    private readonly ILogger _introduced;

                    private readonly ILogger _state;

                    public override dynamic? OverrideMethod() => null;
                }
                """ ),
            "LAMA0876" );

        Assert.Contains( "_state", message, StringComparison.Ordinal );
    }
}
