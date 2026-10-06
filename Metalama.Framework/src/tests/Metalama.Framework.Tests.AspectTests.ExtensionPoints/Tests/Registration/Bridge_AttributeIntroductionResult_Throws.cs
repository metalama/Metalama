// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Tests.ExtensionPoints;

namespace Metalama.Framework.Tests.AspectTests.ExtensionPoints.Tests.Registration.Bridge_AttributeIntroductionResult_Throws;

// The result of an attribute introduction cannot be used as an adviser by an extension.

internal class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.IntroduceAttribute( AttributeConstruction.Create( typeof(ObsoleteAttribute) ) ).TestRegister( "attribute" );
}

// <target>
[TheAspect]
internal class C { }
