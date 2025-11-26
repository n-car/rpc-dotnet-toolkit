# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure
- Multi-targeting support (.NET Standard 2.0, .NET 6.0, .NET 8.0)
- Conditional serialization (Newtonsoft.Json on .NET Standard 2.0, System.Text.Json on .NET 6+)
- RpcRequest, RpcResponse, RpcError types
- RpcOptions and RpcClientOptions configuration
- SerializerFactory with automatic serializer selection
- Safe Mode serialization with type prefixes
- Exception hierarchy for JSON-RPC errors
- Project plan and roadmap

### Changed
- N/A

### Deprecated
- N/A

### Removed
- N/A

### Fixed
- N/A

### Security
- N/A

## [1.0.0] - TBD

Initial release with core features:
- JSON-RPC 2.0 compliance
- Server endpoint
- Client implementation
- Multi-targeting support
- Safe Mode serialization
- Cross-platform compatibility
