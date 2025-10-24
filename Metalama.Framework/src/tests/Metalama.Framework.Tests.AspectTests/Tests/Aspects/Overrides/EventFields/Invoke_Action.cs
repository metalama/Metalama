// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TESTOPTIONS
// @RequiredConstant(NET6_0_OR_GREATER)
// @LanguageVersion(12.0)
#endif

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.EventFields.Invoke_Action
{
    public class OverrideAttribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( invokeTemplate: nameof( InvokeEventTemplate ));
        }

        [Template]
        public void InvokeEventTemplate()
        {
            Console.WriteLine( "Invoke" );
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass<T>
    {
        [Override]
        public event Action Event0;
        
        [Override]
        public event Action<int> Event1;
        
        [Override]
        public event Action<T,int> Event2;
    }
}