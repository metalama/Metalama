# Metalama.Patterns.Xml.Implementation.NamingConvention Namespace

This namespace contains a generalized abstraction of naming convention support. This could be of general use wherever naming conventions are required (if moved to a more general project).

Key features:

* Supports processing a first-success-wins list of naming conventions.
* During processing, gathers information from which diagnostics can later be reported if there is no successful match.
* Abstracts much of the logic required to report useful diagnostics.
* Allows implementations to reduce heap pressure by using readonly structs and generic patterns.

At present the abstraction is tested primarily via integration with the aspects in this project.
