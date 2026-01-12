// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Issue1278_Minimal;

[CompileTime]
public enum Permission
{
    NotSet,
    Read,
    Write
}

public class OverrideAttribute : OverrideFieldOrPropertyAspect
{
    public Permission RequestedPermission { get; set; } = Permission.Read;

    public bool IsSecurable { get; set; } = false;

    [CompileTime]
    private string[] GetActivities()
    {
        return new[] { "Activity1", "Activity2" };
    }

    public override dynamic? OverrideProperty
    {
        get
        {
            // Get compile-time activities and wrap in meta.RunTime
            var activities = meta.RunTime( this.GetActivities() );

            // First if statement with compile-time condition combining two checks
            if ( this.RequestedPermission == Permission.NotSet && this.IsSecurable )
            {
                var result = meta.Proceed();

                return result;
            }
            else
            {
                // Local variable declaration in else block - THIS IS KEY TO REPRODUCING THE BUG
                // The variable is compile-time because it references compile-time properties
                var requestedPermission = this.RequestedPermission != Permission.NotSet ? this.RequestedPermission : Permission.Read;

                // Another if statement using the local variable requestedPermission
                // This triggers StatementCompileTimeVariableFinder.IsVisibleAfterStatement
                if ( requestedPermission == Permission.Write )
                {
                    return null;
                }

                return meta.Proceed();
            }
        }

        set => meta.Proceed();
    }
}

internal class TargetCode
{
    // <target>
    [Override]
    private object? SomeProperty { get; set; }
}