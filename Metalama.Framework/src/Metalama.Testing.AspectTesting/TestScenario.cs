// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Testing.AspectTesting
{
    /// <summary>
    /// Specifies the scenario to simulate when running an aspect test.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This enum controls what aspect of the aspect system is being tested: compilation-time transformation,
    /// code fix application, live template application, design-time behavior, or diff preview.
    /// </para>
    /// <para>
    /// Set the test scenario using the <c>// @TestScenario(scenarioName)</c> comment in your test file,
    /// or configure it via <see cref="TestOptions.TestScenario"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="TestOptions.TestScenario"/>
    /// <seealso href="@aspect-testing"/>
    [PublicAPI]
    public enum TestScenario
    {
        /// <summary>
        /// The default test scenario is that the code is transformed as during compilation.
        /// </summary>
        Default,

        /// <summary>
        /// Tests the application of a code fix. By default, the first suggested code fix is applied.
        /// To apply a different code fix, use the <see cref="TestOptions.AppliedCodeFixIndex"/> property.
        /// To set this scenario in a test, add this comment to your test file: <c>// @TestScenario(CodeFix)</c>.
        /// </summary>
        CodeFix,

        /// <summary>
        /// Tests the application of an aspect as a live template. The test file must contain a single attribute of
        /// type <see cref="TestLiveTemplateAttribute"/> indicating the target and the type of the aspect to be applied.
        /// To enable this option in a test, add this comment to your test file: <c>// @TestScenario(LiveTemplate)</c>.
        /// </summary>
        LiveTemplate,

        /// <summary>
        /// Tests the preview of an aspect as a live template. The test file must contain a single attribute of
        /// type <see cref="TestLiveTemplateAttribute"/> indicating the target and the type of the aspect to be applied.
        /// To enable this option in a test, add this comment to your test file: <c>// @TestScenario(LiveTemplatePreview)</c>.
        /// </summary>
        LiveTemplatePreview,

        /// <summary>
        /// Tests the background code and diagnostic generation at design time.
        /// To enable this option in a test, add this comment to your test file: <c>// @TestScenario(DesignTime)</c>. 
        /// </summary>
        DesignTime,

        /// <summary>
        /// Tests the output of the "diff preview" feature.
        /// To enable this option in a test, add this comment to your test file: <c>// @TestScenario(Preview)</c>. 
        /// </summary>
        Preview
    }
}