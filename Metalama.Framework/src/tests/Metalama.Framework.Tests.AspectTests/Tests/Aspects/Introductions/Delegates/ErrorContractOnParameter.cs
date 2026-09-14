// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.ErrorContractOnParameter;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var introducedDelegate = builder.IntroduceDelegate(
            "Handler",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "value", typeof(int) );
            } );

        // The Invoke method of a delegate reports MethodKind.DelegateInvoke, and the eligibility rule of a contract
        // refuses a parameter of such a method: nothing emits the method, so the contract would be lost.
        var invokeParameter = introducedDelegate.Declaration.Methods.OfName( "Invoke" ).Single().Parameters[0];

        builder.With( invokeParameter ).AddContract( nameof(ContractTemplate) );
    }

    [Template]
    public void ContractTemplate( dynamic? value ) { }
}

// <target>
[Introduction]
public class TargetType { }
