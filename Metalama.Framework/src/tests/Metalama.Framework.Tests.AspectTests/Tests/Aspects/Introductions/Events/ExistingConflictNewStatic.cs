// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Events.ExistingConflictNewStatic
{
    public class IntroductionAttribute : TypeAspect
    {
        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler BaseClassEvent
        {
            add
            {
                meta.InsertComment( "Call base class event." );
                meta.Proceed();
            }

            remove
            {
                meta.InsertComment( "Call base class event." );
                meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler BaseClassEventHiddenByEvent
        {
            add
            {
                meta.InsertComment( "Call derived class event." );
                meta.Proceed();
            }

            remove
            {
                meta.InsertComment( "Call derived class event." );
                meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler DerivedClassEvent
        {
            add
            {
                meta.InsertComment( "Call derived class event." );
                meta.Proceed();
            }

            remove
            {
                meta.InsertComment( "Call derived class event." );
                meta.Proceed();
            }
        }

        [Introduce( WhenExists = OverrideStrategy.New )]
        public static event EventHandler NonExistentEvent
        {
            add
            {
                meta.InsertComment( "Do nothing." );
                meta.Proceed();
            }

            remove
            {
                meta.InsertComment( "Do nothing." );
                meta.Proceed();
            }
        }
    }

    internal abstract class BaseClass
    {
        public static event EventHandler BaseClassEvent
        {
            add { }

            remove { }
        }

        public static event EventHandler BaseClassEventHiddenByEvent
        {
            add { }

            remove { }
        }
    }

    internal class DerivedClass : BaseClass
    {
        public new static event EventHandler BaseClassEventHiddenByEvent
        {
            add { }

            remove { }
        }

        public static event EventHandler DerivedClassEvent
        {
            add { }

            remove { }
        }
    }

    // <target>
    [Introduction]
    internal class TargetClass : DerivedClass { }
}