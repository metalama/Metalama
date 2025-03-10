// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Services;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public abstract partial class SerializationTestsBase : UnitTestClass
    {
        protected ProjectServiceProvider ServiceProvider { get; }

        protected SerializationTestsBase()
        {
            MetalamaEngineModuleInitializer.EnsureInitialized();

            var globalServiceProvider = ServiceProvider<IGlobalService>.Empty;
            globalServiceProvider = globalServiceProvider.WithService( new UserCodeInvoker( globalServiceProvider ) );
            var serviceProvider = ServiceProvider<IProjectService>.Empty.WithNextProvider( globalServiceProvider );
            serviceProvider = serviceProvider.WithService( new BuiltInSerializerFactoryProvider( serviceProvider ) );
            this.ServiceProvider = serviceProvider;
        }

        protected override TestContext CreateTestContextCore( TestContextOptions contextOptions, IAdditionalServiceCollection services )
            => new SerializationTestContext( contextOptions, services );

        // ReSharper disable ExplicitCallerInfoArgument

        [MustDisposeResource]
        protected new SerializationTestContext CreateTestContext(
            [CallerFilePath] string? callerFile = null,
            [CallerMemberName] string? callerMemberName = null )
            => (SerializationTestContext) base.CreateTestContext( callerFile, callerMemberName );

        [MustDisposeResource]
        protected SerializationTestContext CreateTestContextWithCode(
            string code,
            [CallerFilePath] string? callerFile = null,
            [CallerMemberName] string? callerMemberName = null )
            => (SerializationTestContext) base.CreateTestContext( new SerializationTestContextOptions { Code = code }, null, callerFile, callerMemberName );

        // ReSharper restore ExplicitCallerInfoArgument

        protected T? TestSerialization<T>( T? instance, Func<T?, T?, bool>? assert = null, bool testEquality = true )
        {
            using var testContext = this.CreateTestContext();

            return TestSerialization( testContext, instance, assert, testEquality );
        }

        protected static T? TestSerialization<T>(
            SerializationTestContext testContext,
            T? instance,
            Func<T?, T?, bool>? assert = null,
            bool testEquality = true )
        {
            var memoryStream = new MemoryStream();
            testContext.Serializer.Serialize( instance, memoryStream );
            memoryStream.Seek( 0, SeekOrigin.Begin );
            var deserializedObject = (T?) testContext.Serializer.Deserialize( memoryStream );

            var newCol = deserializedObject as ICollection;

            if ( assert != null )
            {
                assert( instance, deserializedObject );
            }
            else if ( testEquality )
            {
                if ( instance is ICollection orgCol )
                {
                    Assert.Equal( orgCol, newCol );
                }
                else
                {
                    Assert.Equal( instance, deserializedObject );
                }
            }

            return deserializedObject;
        }

        protected T SerializeDeserialize<T>( T value )
        {
            using var testContext = this.CreateTestContext();

            return SerializeDeserialize( value, testContext );
        }

        protected static T SerializeDeserialize<T>( T value, SerializationTestContext testContext )
        {
            var memoryStream = new MemoryStream();

            testContext.Serializer.Serialize( value!, memoryStream );

            memoryStream.Seek( 0, SeekOrigin.Begin );
            var deserialized = (T) testContext.Serializer.Deserialize( memoryStream ).AssertNotNull();

            return deserialized;
        }
    }
}