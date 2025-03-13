// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Consumption
{
    /// <summary>
    /// Information about a license relevant to licensed features consumption.
    /// </summary>
    [PublicAPI]
    public sealed record LicenseConsumptionProperties
    {
        /// <summary>
        /// Gets the namespace constraint of the license.
        /// Gets <c>null</c> if there is no namespace constraint.
        /// </summary>
        public string? LicensedNamespace { get; }

        /// <summary>
        /// Gets the product licensed by the license.
        /// </summary>
        public LicenseProduct LicenseProduct { get; }

        /// <summary>
        /// Gets the type of the license.
        /// </summary>
        public LicenseType LicenseType { get; }

        /// <summary>
        /// Gets the displayable name of the license shown in diagnostics and trace messages.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the minimal PostSharp version the license can be used with.
        /// </summary>
        /// <remarks>
        /// Doesn't apply to products not based on PostSharp. (E.g. Metalama.)
        /// </remarks>
        [Obsolete]
        public Version MinPostSharpVersion { get; }

        /// <summary>
        /// Gets a value indicating whether the license is redistributable.
        /// </summary>
        [Obsolete]
        public bool IsRedistributable { get; }

        /// <summary>
        /// Gets the string representation of the license if the license has it.
        /// </summary>
        public string? LicenseString { get; }

        /// <summary>
        /// Gets a value indicating whether the license usage can be audited.
        /// </summary>
        public bool IsAuditable { get; }

        public DateTime? SubscriptionEndDate { get; }

        public SubscriptionStatus SubscriptionStatus { get; }

        public LicenseGeneration Generation { get; }

        public ServicingPhase ServicingPhase { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseConsumptionProperties"/> class.
        /// </summary>
        /// <param name="licenseProduct">The product licensed by the license.</param>
        /// <param name="licenseType">The type of the license.</param>
        /// <param name="licensedRequirement">Requirement available by the license.</param>
        /// <param name="licensedNamespace">Namespace constraint of the license. <c>null</c> if there is no namespace constraint.</param>
        /// <param name="displayName">The displayable name of the license shown in diagnostics and trace messages.</param>
        /// <param name="minPostSharpVersion">Minimal PostSharp version the license can be used with. Doesn't apply to products not based on PostSharp. (E.g. Metalama.)</param>
        /// <param name="licenseString">The string representation of the license, if exists. <c>null</c> if the license doesn't have a string representation.</param>
        /// <param name="isRedistributable">Indicates whether the license is redistributable.</param>
        /// <param name="maxAspectsCount">The number of aspects allowed to be used.</param>
        public LicenseConsumptionProperties(
            LicenseProduct licenseProduct,
            LicenseType licenseType,
            string? licensedNamespace,
            string displayName,
            Version minPostSharpVersion,
            string? licenseString,
            bool isRedistributable,
            bool isAuditable,
            DateTime? subscriptionEndDate,
            SubscriptionStatus subscriptionStatus,
            LicenseGeneration generation,
            ServicingPhase servicingPhase )
        {
            this.LicenseProduct = licenseProduct;
            this.LicenseType = licenseType;
            this.DisplayName = displayName;
            this.LicenseString = licenseString;
            this.IsAuditable = isAuditable;
            this.SubscriptionEndDate = subscriptionEndDate;
            this.SubscriptionStatus = subscriptionStatus;
            this.Generation = generation;
            this.ServicingPhase = servicingPhase;
            this.LicensedNamespace = licensedNamespace;

#pragma warning disable CS0612 // Type or member is obsolete
            this.IsRedistributable = isRedistributable;
            this.MinPostSharpVersion = minPostSharpVersion;
#pragma warning restore CS0612 // Type or member is obsolete
        }

        /// <summary>
        /// Gets the displayable name of the license.
        /// </summary>
        /// <returns>The displayable name of the license.</returns>
        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}