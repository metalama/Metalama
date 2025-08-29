// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Events.RaiseInvalidTemplate
{
    public class Override1Attribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate ));
        }

        [Template]
        public void InvokeEventTemplate(int x)
        {
            meta.Proceed();
        }
    }

    public class Override2Attribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate ) );
        }

        [Template]
        public void InvokeEventTemplate( EventHandler handler, int x )
        {
            meta.Proceed();
        }
    }

    public class Override3Attribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate ) );
        }

        [Template]
        public void InvokeEventTemplate( EventHandler handler, object sender, EventArgs args, int x )
        {
            meta.Proceed();
        }
    }

    public class Override4Attribute : EventAspect
    {
        public override void BuildAspect( IAspectBuilder<IEvent> builder )
        {
            builder.OverrideAccessors( null, null, nameof( InvokeEventTemplate ) );
        }

        [Template]
        public void InvokeEventTemplate( EventHandler handler, object sender, int args )
        {
            meta.Proceed();
        }
    }

    // <target>
    internal class TargetClass 
    {
        [Override1]
        public event EventHandler Event1
        {
            add { }
            remove { }
        }

        [Override2]
        public event EventHandler Event2
        {
            add { }
            remove { }
        }

        [Override3]
        public event EventHandler Event3
        {
            add { }
            remove { }
        }

        [Override4]
        public event EventHandler Event4
        {
            add { }
            remove { }
        }
    }
}