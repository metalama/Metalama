// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RunAnalyzers(false)
#endif

// The @RunAnalyzers(false) directive disables the option that the metalamaTests.json file of this directory enables, so
// LAMA0870 is not reported.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.TestFramework.RunAnalyzersEnabled.RunAnalyzers_DisabledByDirective;

// The aspect stores a declaration of the code model in a property. An aspect instance outlives the compilation, so the
// DurableContractAnalyzer reports LAMA0870 on the property when it runs.
public class RegisterEntityAspect : TypeAspect
{
    public INamedType? Type { get; private set; }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        this.Type = builder.Target;
    }
}

// <target>
[RegisterEntityAspect]
public class Customer { }
