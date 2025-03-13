// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Project;
using System;
using System.Collections.Immutable;
using System.Text;

namespace Metalama.Framework.Eligibility.Implementation
{
    internal sealed class OrEligibilityRule<T> : IEligibilityRule<T>
        where T : class
    {
        private readonly ImmutableArray<IEligibilityRule<T>> _predicates;

        public OrEligibilityRule( ImmutableArray<IEligibilityRule<T>> predicates )
        {
            this._predicates = predicates;
        }

        public EligibleScenarios GetEligibility( T obj )
        {
            var eligibility = EligibleScenarios.None;

            foreach ( var predicate in this._predicates )
            {
                eligibility |= predicate.GetEligibility( obj );

                if ( eligibility == EligibleScenarios.All )
                {
                    return EligibleScenarios.All;
                }
            }

            return eligibility;
        }

        public FormattableString GetIneligibilityJustification(
            EligibleScenarios requestedEligibility,
            IDescribedObject<T> describedObject )
        {
            switch ( this._predicates.Length )
            {
                case 0:
                    return $"the Or condition group is empty";

                case 1:
#pragma warning disable CS8603
                    return this._predicates[0].GetIneligibilityJustification( requestedEligibility, describedObject );
#pragma warning restore CS8603

                default:
                    {
                        StringBuilder stringBuilder = new();
                        stringBuilder.Append( "none of these conditions was fulfilled: { " );
                        var letter = 'a';

                        for ( var i = 0; i < this._predicates.Length; i++ )
                        {
                            var predicate = this._predicates[i];
                            var justification = predicate.GetIneligibilityJustification( requestedEligibility, describedObject );

                            if ( justification != null )
                            {
                                if ( i > 0 )
                                {
                                    stringBuilder.Append( ", or " );
                                }

                                stringBuilder.Append( '(' );
                                stringBuilder.Append( letter );
                                stringBuilder.Append( ')' );
                                stringBuilder.Append( ' ' );

                                stringBuilder.Append( justification.ToString( MetalamaExecutionContext.Current.FormatProvider ) );
                            }

                            letter++;
                        }

                        stringBuilder.Append( " }" );

                        return $"{stringBuilder}";
                    }
            }
        }
    }
}