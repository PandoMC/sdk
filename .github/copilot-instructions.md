# Copilot Instructions

## SDK README files

Every SDK in this repository **must** have a `README.md` in its top-level folder (e.g. `dotnet/README.md`, `node/README.md`).

Use [dotnet/README.md](../dotnet/README.md) as the canonical template. A well-formed SDK README must cover:

- A one-line description of the SDK and a link to the Mission:Control website.
- **Requirements** — minimum runtime/SDK version.
- **Installation** — the package manager command to add the package.
- **Getting started** — how to build a client with credentials, how to target Sandbox vs Production, and (where applicable) a dependency-injection example.
- **Usage examples** — short, self-contained snippets for the most common operations (list products, check stock, reserve → claim, cancel a reservation, etc.).

When a section is updated in one SDK README (e.g. a new usage example is added to `dotnet/README.md`), apply the equivalent change — translated to the target language's syntax and conventions — to **all other SDK README files** as well.

## Root README

[README.md](../README.md) lists every available SDK under the **SDKs** heading.  
Whenever a new SDK is added to the repository, add a corresponding entry there.  
Example: if a `go/` SDK is added, append `- [Go](go/README.md)` to the list.
