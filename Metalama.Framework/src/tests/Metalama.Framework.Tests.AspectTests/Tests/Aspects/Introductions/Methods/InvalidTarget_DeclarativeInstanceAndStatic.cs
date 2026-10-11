// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Methods.InvalidTarget_DeclarativeInstanceAndStatic
{
    /*
     * Tests that an aspect with both an instance and a static declarative introduction cannot be applied to a static type,
     * regardless of the order and of the names of the members (#2152).
     */

    public class InstanceFirstAttribute : TypeAspect
    {
        [Introduce]
        public int A_Instance()
        {
            return meta.Proceed();
        }

        [Introduce]
        public static int B_Static()
        {
            return meta.Proceed();
        }
    }

    public class StaticFirstAttribute : TypeAspect
    {
        [Introduce]
        public static int A_Static()
        {
            return meta.Proceed();
        }

        [Introduce]
        public int B_Instance()
        {
            return meta.Proceed();
        }
    }

    public class InstanceFirstReverseNamesAttribute : TypeAspect
    {
        [Introduce]
        public int B_Instance()
        {
            return meta.Proceed();
        }

        [Introduce]
        public static int A_Static()
        {
            return meta.Proceed();
        }
    }

    public class StaticFirstReverseNamesAttribute : TypeAspect
    {
        [Introduce]
        public static int B_Static()
        {
            return meta.Proceed();
        }

        [Introduce]
        public int A_Instance()
        {
            return meta.Proceed();
        }
    }

    // <target>
    [InstanceFirst]
    [StaticFirst]
    [InstanceFirstReverseNames]
    [StaticFirstReverseNames]
    internal static class StaticTargetClass { }
}
