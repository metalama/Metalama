// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation
{
    public interface IBaseInterface
    {
        void BaseMethod();

        int BaseProperty { get; set; }

        event EventHandler? BaseEvent;
    }

    public interface IInterface : IBaseInterface
    {
        void Method();

        int Property { get; set; }

        event EventHandler? Event;
    }

    [CompileTime]
    public class AdviceResultTemplates : ITemplateProvider
    {
        [Template]
        public void WitnessTemplate(
            [CompileTime] IReadOnlyCollection<IInterfaceImplementationResult> types,
            [CompileTime] IReadOnlyCollection<IInterfaceMemberImplementationResult>? members )
        {
            foreach (var type in types)
            {
                Console.WriteLine( $"InterfaceType: {type.InterfaceType}, Action: {type.Outcome}" );
            }

            foreach (var member in members ?? Array.Empty<IInterfaceMemberImplementationResult>())
            {
                Console.WriteLine( $"Member: {member.InterfaceMember}, Action: {member.Outcome}, Target: {member.TargetMember}" );
            }
        }
    }
}