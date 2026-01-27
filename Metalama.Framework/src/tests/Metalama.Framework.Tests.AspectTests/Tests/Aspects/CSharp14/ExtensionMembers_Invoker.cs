// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @RequiredConstant(ROSLYN_5_0_0_OR_GREATER)
// @RequiredConstant(NET8_0_OR_GREATER)
// @FormatOutput
#endif

#if ROSLYN_5_0_0_OR_GREATER

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp14.ExtensionMembers_Invoker;

/// <summary>
/// Tests invoking extension members from an aspect template using the invoker API.
/// </summary>
internal class InvokeExtensionMemberAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        var declaringType = meta.Target.Method.DeclaringType;

        // Find the extension block and invoke its members.
        foreach ( var extensionBlock in declaringType.ExtensionBlocks )
        {
            foreach ( var method in extensionBlock.Methods )
            {
                if ( !method.IsStatic && method.Parameters.Count == 0 )
                {
                    // Invoke instance extension method with the receiver from meta.Target.Parameters.
                    method.WithObject( meta.Target.Parameters[0] ).Invoke();
                }
                else if ( method.IsStatic && method.Parameters.Count == 0 )
                {
                    // Invoke static extension method.
                    method.Invoke();
                }
            }

            foreach ( var property in extensionBlock.Properties )
            {
                if ( !property.IsStatic )
                {
                    // Access instance extension property.
                    var value = property.WithObject( meta.Target.Parameters[0] ).Value;
                    Console.WriteLine( value );
                }
                else
                {
                    // Access static extension property.
                    var value = property.Value;
                    Console.WriteLine( value );
                }
            }
        }

        return default;
    }
}

// <target>
internal static class C
{
    extension( TestClass receiver )
    {
        public void InstanceMethod() => Console.WriteLine( "Instance method called" );

        public static void StaticMethod() => Console.WriteLine( "Static method called" );

        public string InstanceProperty => "Instance property value";

        public static string StaticProperty => "Static property value";
    }

    [InvokeExtensionMemberAspect]
    public static void TestMethod( TestClass target )
    {
    }
}

internal class TestClass { }

#endif
