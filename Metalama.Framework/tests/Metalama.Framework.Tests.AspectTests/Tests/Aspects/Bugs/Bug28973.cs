using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Aspects.Bugs.Bug28973
{
    // <target>
    internal class TargetCode
    {
        [Import]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private IFormatProvider FormatProvider { get; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }

    internal class ServiceLocator : IServiceProvider
    {
        private static readonly ServiceLocator _instance = new();
        private readonly Dictionary<Type, object> _services = new();

        public static IServiceProvider ServiceProvider => _instance;

        object? IServiceProvider.GetService( Type serviceType )
        {
            _services.TryGetValue( serviceType, out var value );

            return value;
        }

        public static void AddService<T>( T service ) => _instance._services[typeof(T)] = service!;
    }

    internal class ImportAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get => ServiceLocator.ServiceProvider.GetService( meta.Target.FieldOrProperty.Type.ToType() );

            set => meta.Proceed();
        }
    }
}