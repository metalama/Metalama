# Security Policy

Metalama is a compiler that runs locally on your development machine or on a build agent.

It does not access the network except for telemetry purposes. Please check our [privacy policy](https://www.postsharp.net/company/legal/privacy-policy) for details about data collection.

We regularly audit our dependencies and use automated tools to scan for known vulnerabilities. Any flagged issues are promptly addressed. Metalama does not process confidential data other than source code, which always remains on the device executing Metalama.

As a result, potential vulnerabilities in Metalama or its dependencies should have minimal impact. Nevertheless, we strive to keep our dependencies up to date.

The most critical risk associated with Metalama would be a supply chain attack where malicious code is injected into Metalama and affects its users. To mitigate this risk, we implement the following measures:

- The product is open source, with some extensions being source-available.
- We sign our binaries using an Authenticode key on a secure and isolated device, separate from build agents and development machines. Before signing, we scan the binaries for malware.
- We produce deterministic builds, enabling customers to independently build and compare the binaries.
- We regularly audit our dependencies and use automated tools (e.g., NuGet Package Vulnerability Scanner, GitHub dependabot, GitHub code scanning) to scan for known vulnerabilities. Any flagged issues are promptly addressed.

## Supported Versions

In the event a vulnerability is reported, we will address it in all supported versions.

Ensure that you are using a supported version. We maintain a list of supported versions on [this page](https://www.postsharp.net/support/policies/versions).

## Reporting a Vulnerability

To report a vulnerability, create an issue in the [Metalama](https://github.com/metalama/Metalama) repository (without including details that could exploit the vulnerability) and simultaneously send an email to <hello@postsharp.net> with full details.

We aim to acknowledge vulnerability reports within 24 hours and provide a resolution or mitigation plan within 3 days, depending on the severity of the issue.

If you do not receive a prompt response, please [contact us by phone](https://www.postsharp.net/contact).
