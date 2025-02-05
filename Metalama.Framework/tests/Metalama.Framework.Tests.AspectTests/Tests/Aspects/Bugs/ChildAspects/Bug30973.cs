// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;
using System.Linq;

#pragma warning disable CS0067

/*
 * #30973 Introducing interfaces causes AssertionFailedException
 */

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug30973
{
    public class LoggingAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"Executing {meta.Target.Method.ToDisplayString()}" );

            return meta.Proceed();
        }
    }

    public class FieldOrPropertyLoggingAspect : FieldOrPropertyAspect
    {
        public override void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
        {
            builder.Override( nameof(this.OverrideProperty) );
        }

        [Template]
        public dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( $"Executing {meta.Target.Method.ToDisplayString()}" );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( $"Executing {meta.Target.Method.ToDisplayString()}" );
                meta.Proceed();
            }
        }
    }

    public interface IIntroducedInterface
    {
        int InterfaceMethod();

        event EventHandler InterfaceEvent;

        event EventHandler? InterfaceEventField;

        int Property { get; set; }

        string? AutoProperty { get; set; }
    }

    public class IntroductionAttribute : TypeAspect
    {
        [Introduce]
        public void IntroducedMethod() { }
    }

    public class InterfaceIntroductionAttribute : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            builder.ImplementInterface( typeof(IIntroducedInterface) );
        }

        [InterfaceMember( IsExplicit = true )]
        public int InterfaceMethod()
        {
            Console.WriteLine( "This is introduced interface member." );

            return meta.Proceed();
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? InterfaceEvent
        {
            add
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }

            remove
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }
        }

        [InterfaceMember( IsExplicit = true )]
        public event EventHandler? InterfaceEventField;

        [InterfaceMember( IsExplicit = true )]
        public int Property
        {
            get
            {
                Console.WriteLine( "This is introduced interface member." );

                return meta.Proceed();
            }

            set
            {
                Console.WriteLine( "This is introduced interface member." );
                meta.Proceed();
            }
        }

        [InterfaceMember( IsExplicit = true )]
        public string? AutoProperty { get; set; }
    }

    public class TestProjectFabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectMany(
                    p =>
                        p.Types
                            .Where( t => t is { Name: nameof(BackorderMode) } )
                            .SelectMany( t => t.Methods.Where( m => !m.IsImplicitlyDeclared ) ) )
                .AddAspect<LoggingAspect>();

            amender.SelectMany(
                    p =>
                        p.Types
                            .Where( t => t is { Name: nameof(BackorderMode) } )
                            .SelectMany( t => t.Fields.Where( m => !m.IsImplicitlyDeclared ) )
                            .Cast<IFieldOrProperty>() )
                .AddAspect<FieldOrPropertyLoggingAspect>();

            amender.SelectMany( p => p.Types.Where( t => t is { Name: nameof(BackorderMode) } ) ).AddAspect<IntroductionAttribute>();
            amender.SelectMany( p => p.Types.Where( t => t is { Name: nameof(BackorderMode) } ) ).AddAspect<InterfaceIntroductionAttribute>();
        }
    }

    /// <summary>
    /// Represents a backorder mode
    /// </summary>
    public enum BackorderMode
    {
        /// <summary>
        /// No backorders
        /// </summary>
        NoBackorders = 0,

        /// <summary>
        /// Allow qty below 0
        /// </summary>
        AllowQtyBelow0 = 1,

        /// <summary>
        /// Allow qty below 0 and notify customer
        /// </summary>
        AllowQtyBelow0AndNotifyCustomer = 2
    }
}