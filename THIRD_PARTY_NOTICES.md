# Third-Party Notices

The `linux_amd64` release of `terraform-provider-beyondtrust-secretsafe` is built as a fully static binary that statically links the following third-party software:

## OpenSSL

- **Project:** OpenSSL (https://www.openssl.org/)
- **Version:** the `openssl-libs-static` package available in the `mcr.microsoft.com/dotnet/sdk:10.0-alpine` build image at release time (OpenSSL 3.x)
- **License:** Apache License, Version 2.0
- **License text:** [`THIRD_PARTY_LICENSES/OPENSSL-LICENSE.txt`](./THIRD_PARTY_LICENSES/OPENSSL-LICENSE.txt) (also bundled inside the `linux_amd64` release zip)

OpenSSL is linked statically via the .NET Native AOT property `StaticOpenSslLinking=true`. Statically-linked redistribution of OpenSSL under Apache 2.0 requires that the license text accompany the binary; that requirement is fulfilled by including `OPENSSL-LICENSE` next to the provider binary inside the Linux release zip.

The Windows release binary uses Schannel (the OS-provided TLS stack) and does not include OpenSSL.
