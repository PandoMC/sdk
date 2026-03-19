# Mission:Control Partner SDK

## SDKs

- [.NET](dotnet/README.md)
- [.Node](node/README.md)

## How to update the SDKs

Run `update_sdks.ps1` in the root of the repository to update all SDKs.

## Publishing a new SDK version

Each SDK is published independently via GitHub Actions, triggered by pushing a version tag with a language prefix.

| SDK  | Tag pattern | Published to |
| ---- | ----------- | ------------ |
| .NET | `dotnet/v*` | NuGet        |
| Node | `node/v*`   | npm          |

**Steps:**

1. Merge all changes for the release into `main`.
2. Create and push a tag for the SDK you want to release:
   ```sh
   git tag dotnet/v1.2.0
   git push origin dotnet/v1.2.0
   ```
3. GitHub Actions automatically picks up the tag, builds the SDK and publishes the package to the appropriate registry. The other SDKs are unaffected.

Use [semantic versioning](https://semver.org): `MAJOR.MINOR.PATCH`.
