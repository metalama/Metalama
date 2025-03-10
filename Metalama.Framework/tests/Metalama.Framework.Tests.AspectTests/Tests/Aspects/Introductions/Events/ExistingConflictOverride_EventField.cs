// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictOverride_EventField
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? ExistingBaseEvent;

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? ExistingEvent;

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static event EventHandler? ExistingEvent_Static;

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public event EventHandler? NotExistingEvent;

        [Introduce( WhenExists = OverrideStrategy.Override )]
        public static event EventHandler? NotExistingEvent_Static;
    }

    internal class BaseClass
    {
        public virtual event EventHandler? ExistingBaseEvent
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : BaseClass
    {
        public event EventHandler? ExistingEvent
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }

        public static event EventHandler? ExistingEvent_Static
        {
            add
            {
                Console.WriteLine( "This is original event." );
            }

            remove
            {
                Console.WriteLine( "This is original event." );
            }
        }
    }
}