// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue1278_InitializerWithLambda;

[CompileTime]
public enum Permission
{
    NotSet,
    Read,
    Write
}

public class TheAspect : TypeAspect
{
    public static Permission RequestedPermission { get; set; } = Permission.Read;

    [Introduce]
    public Func<int> IntroducedProperty { get; } = meta.RunTime(
        () =>
        {
            if ( RequestedPermission == Permission.NotSet )
            {
                return 0;
            }
            else
            {
                var requestedPermission = RequestedPermission != Permission.NotSet ? RequestedPermission : Permission.Read;

                if ( requestedPermission == Permission.Write )
                {
                    return 1;
                }

                return 2;
            }
        } );
}

[TheAspect]
internal class TargetCode { }