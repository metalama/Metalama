// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// This test verifies that there is no error when writing the HTML file for an introduced syntax tree,
// but it does not test the HTML itself.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction;

[assembly: IntroduceTopLevelClass]
namespace Metalama.Framework.IntegrationTests.TestFramework.Html.TopLevelClassIntroduction
{
    public class IntroduceTopLevelClassAttribute : CompilationAspect
    {
        public override void BuildAspect( IAspectBuilder<ICompilation> builder )
        {
            var ns = builder.WithNamespace( "Some.Namespace" );
            ns.IntroduceClass( "SomeClass" );
        }
    }
}