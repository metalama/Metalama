// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Testing;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Licensing
{
    public abstract class LicensingCommandsTestsBase : CommandsTestsBase
    {
        protected LicensingCommandsTestsBase( ITestOutputHelper logger )
            : base( logger )
        {
            this.UserDeviceDetection.IsInteractiveDevice = true;
        }

        protected static TestLicenseKeyProvider LicenseKeyProvider { get; } = new();

        /// <summary>
        /// The name of a group of license keys that no version of Metalama supports today. A later version writes
        /// such a group when it registers a license key that the current version cannot consume. See issue #1922.
        /// </summary>
        protected const string UnsupportedVersion = "2099.0";

        /// <summary>
        /// A license key of a format that the current version cannot parse. It stands for a license key that only
        /// Metalama <see cref="UnsupportedVersion"/> understands, so a command that meets it in an unsupported group
        /// proves that the group is skipped before its license keys are deserialized.
        /// </summary>
        private const string _unparsableLicenseKey = "999-THIS-LICENSE-KEY-REQUIRES-A-LATER-VERSION";

        /// <summary>
        /// Adds a group of license keys that the current version does not support to the licensing configuration,
        /// beside the license keys that are already registered.
        /// </summary>
        protected void AddUnsupportedLicenseGroup() => this.AddLicenseGroup( UnsupportedVersion, _unparsableLicenseKey );
    }
}
