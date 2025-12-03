// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using ServiceFactory.Contracts;

namespace ServiceFactory.Consumer;

/// <summary>
/// An aspect that uses the custom service to introduce a greeting method.
/// </summary>
public class GreetingAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<string> _serviceNotFound =
        new( "SERVICE001", Severity.Error, "Service '{0}' not found." );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var service = builder.Project.ServiceProvider.GetService<IMyService>();

        if ( service == null )
        {
            builder.Diagnostics.Report( _serviceNotFound.WithArguments( nameof( IMyService ) ) );

            return;
        }

        var greeting = service.GetGreeting( builder.Target.Name );

        builder.IntroduceMethod( nameof( SayHello ), args: new { greeting } );
    }

    [Template]
    public static void SayHello( [CompileTime] string greeting )
    {
        Console.WriteLine( greeting );
    }
}
