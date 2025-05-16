## Security Policies

This project adheres to the security policies of its core maintainer, PostSharp Technologies. For more details, please visit [PostSharp Security Policies](https://www.postsharp.net/support/policies/security).

## Security Frameworks

### OpenSSF Best Practices

[![OpenSSF Best Practices](https://www.bestpractices.dev/projects/10558/badge)](https://www.bestpractices.dev/projects/10558)

We follow the Open Source Security Foundation (OpenSSF) best practices. For more details, refer to the [self-assessment report](https://www.bestpractices.dev/projects/10558).

### OpenSSF Scorecard

[![OpenSSF Scorecard](https://api.securityscorecards.dev/projects/github.com/metalama/Metalama/badge)](https://securityscorecards.dev/viewer/?uri=github.com/metalama/Metalama)

We implement most recommendations from the OpenSSF Scorecard benchmark. See the [detailed report](https://securityscorecards.dev/viewer/?uri=github.com/metalama/Metalama) for more information.

## Best Practice Exceptions

The following best practices are not implemented in this project:

### Fuzzing

Fuzzing (or fuzz testing) is a software testing technique that automatically feeds a program with random, malformed, or unexpected input data to identify crashes, hangs, or security vulnerabilities (e.g., buffer overflows, assertion failures).

This project does **not handle or parse untrusted input** from external sources. All data processed originates from trusted, internal, or authenticated systems. Therefore, fuzz testing, which is primarily aimed at finding crashes or unexpected behavior from malformed or adversarial input, is **not applicable or necessary** in this context.

## Static Analysis

We use the following code scanners on release and CI branches:

- [GitHub CodeQL](https://codeql.github.com/)
- [OpenSSF Scorecard](https://securityscorecards.dev)
- [DevSkim](https://github.com/microsoft/DevSkim)