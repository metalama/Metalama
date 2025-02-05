using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug28905
{
    internal class ImportAttribute : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                // Get the property value.
                var value = meta.Proceed();

                if (value == null)
                {
                    // Call the service locator.
                    value = meta.Cast( meta.Target.FieldOrProperty.Type, ServiceLocator.ServiceProvider.GetService( meta.Target.Property.Type.ToType() ) );

                    // Set the field/property to the new value.
                    meta.Target.Property.Value = value
                                                 ?? throw new InvalidOperationException( $"Cannot get a service of type {meta.Target.Property.Type}." );
                }

                return value;
            }

            set => meta.Proceed();
        }
    }

    // <target>
    internal class Yack
    {
        [Import]
        private IGreetingService? _service { get; set; }
    }

    internal interface IGreetingService
    {
        void Greet( string name );
    }

    internal class ServiceLocator
    {
        public static readonly IServiceProvider ServiceProvider = null!;
    }
}