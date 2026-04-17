# ADR-0004: Central package management (CPM)

## Status

Accepted

## Context

Eight projects reference overlapping NuGet packages. Without centralized version management, version drift across projects causes subtle runtime mismatches and makes security patching tedious.

## Decision

Enable MSBuild Central Package Management via `Directory.Packages.props` with `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`. All `<PackageReference>` elements across every `.csproj` must omit the `Version` attribute. Versions are declared once in `Directory.Packages.props`. The build fails with `NU1604` if any project pins its own version.

## Consequences

- Single file to update for security patches or upgrades.
- New projects get the correct version automatically by adding only `<PackageReference Include="..." />`.
- Developers cannot accidentally diverge package versions between projects.
