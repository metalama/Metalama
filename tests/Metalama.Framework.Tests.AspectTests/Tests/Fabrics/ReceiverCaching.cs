// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Fabrics;

/*
 * The purpose of this test is to verify that there are no duplicates when the receiver is cached.
 * We test several levels of caching with validators and aspect instances.
 */

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.ReceiverCaching
{
    internal class Fabric : NamespaceFabric
    {
        private static readonly DiagnosticDefinition<string> _warning1 = new( "MY001", Severity.Warning, "Warning 1: {0}." );
        private static readonly DiagnosticDefinition<string> _warning2 = new( "MY002", Severity.Warning, "Warning 2: {0}." );

        public override void AmendNamespace( INamespaceAmender amender )
        {
            var types = amender
                .SelectMany( c => c.DescendantsAndSelf() )
                .SelectMany( t => t.Types );

            types.ReportDiagnostic( m => _warning1.WithArguments( m.ToDisplayString() ) );
            types.ReportDiagnostic( m => _warning2.WithArguments( m.ToDisplayString() ) );

            var methods = types
                .SelectMany( t => t.Methods )
                .Where( m => m.ReturnType.IsConvertibleTo( typeof(string) ) );

            methods.AddAspect<MethodAspect>();
            methods.ReportDiagnostic( m => _warning1.WithArguments( m.ToDisplayString() ) );
            methods.ReportDiagnostic( m => _warning2.WithArguments( m.ToDisplayString() ) );

            var properties = types
                .SelectMany( t => t.Properties )
                .Where( m => m.Type.IsConvertibleTo( typeof(string) ) );

            properties.AddAspect<PropertyAspect>();
            properties.ReportDiagnostic( m => _warning1.WithArguments( m.ToDisplayString() ) );
            properties.ReportDiagnostic( m => _warning2.WithArguments( m.ToDisplayString() ) );
        }
    }

    internal class MethodAspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( $"overridden instances: {meta.AspectInstance.SecondaryInstances.Length + 1}" );

            return meta.Proceed();
        }
    }

    internal class PropertyAspect : OverrideFieldOrPropertyAspect
    {
        public override dynamic? OverrideProperty
        {
            get
            {
                Console.WriteLine( $"overridden instances: {meta.AspectInstance.SecondaryInstances.Length + 1}" );

                return meta.Proceed();
            }
            set
            {
                Console.WriteLine( $"overridden instances: {meta.AspectInstance.SecondaryInstances.Length + 1}" );

                meta.Proceed();
            }
        }
    }

    // <target>
    internal class TargetCode
    {
        private int Method1( int a ) => a;

        private string Method2( string s ) => s;

        private string Property1 => "";
    }

    // <target>
    namespace Sub
    {
        internal class AnotherClass
        {
            private int Method1( int a ) => a;

            private string Method2( string s ) => s;

            private string Property1 => "";
        }
    }
}