// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

#pragma warning disable CS8618, CS0169, CS0649

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug1579;

public interface IMessage { }

public static class MessageRouter
{
    public static void Register( Type handlerType, Type messageType ) { }
}

public class RegisterMessageHandlerAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var selfType = builder.Target.MakeGenericInstance(
            builder.Target.TypeParameters.ToArray<IType>() );

        builder.AddInitializer(
            nameof(RegisterHandler),
            InitializerKind.BeforeTypeConstructor,
            args: new
            {
                TSelf = selfType,
                TMessage = builder.Target.TypeParameters[0]
            } );
    }

    [Template]
    private static void RegisterHandler<[CompileTime] TSelf, [CompileTime] TMessage>()
    {
        MessageRouter.Register( typeof(TSelf), typeof(TMessage) );
    }
}

// <target>
[RegisterMessageHandler]
public partial class Handler<TMessage> where TMessage : IMessage { }
