// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing
{
    [PublicAPI]
    public class LicenseRequirement
    {
        private readonly string _name;

        private readonly ImmutableArray<LicensedProduct> _eligibleProducts;

        public bool IsFulfilledBy( LicenseConsumptionData license )
            => license.LicenseType switch
            {
                LicenseType.Essentials => false, // This is for PostSharp Essentials only
                LicenseType.OpenSourceRedistribution => true,
                LicenseType.CommercialRedistribution => true,
                _ => this._eligibleProducts.Contains( license.LicensedProduct )
            };

        private LicenseRequirement( string name, params LicensedProduct[] eligibleProducts )
        {
            this._name = name;
            this._eligibleProducts = eligibleProducts.ToImmutableArray();
        }

        public override string ToString()
        {
            return this._name;
        }

        public static readonly LicenseRequirement Free = new(
            "Free",
            LicensedProduct.MetalamaFree,
            LicensedProduct.MetalamaStarter,
            LicensedProduct.MetalamaProfessional,
            LicensedProduct.MetalamaUltimate,
            LicensedProduct.Framework,
            LicensedProduct.Ultimate );

        public static readonly LicenseRequirement Starter = new(
            "Starter",
            LicensedProduct.MetalamaStarter,
            LicensedProduct.MetalamaProfessional,
            LicensedProduct.MetalamaUltimate,
            LicensedProduct.Framework,
            LicensedProduct.Ultimate );

        public static readonly LicenseRequirement Professional = new(
            "Professional",
            LicensedProduct.MetalamaProfessional,
            LicensedProduct.MetalamaUltimate,
            LicensedProduct.Framework,
            LicensedProduct.Ultimate );

        public static readonly LicenseRequirement Ultimate = new(
            "Ultimate",
            LicensedProduct.MetalamaUltimate,
            LicensedProduct.Ultimate );
    }
}