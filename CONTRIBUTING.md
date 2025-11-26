# Contributing to RPC .NET Toolkit

Thank you for your interest in contributing! 🎉

## 🚀 Getting Started

1. **Fork** the repository
2. **Clone** your fork locally
3. **Create** a new branch for your feature/fix
4. **Make** your changes
5. **Test** thoroughly
6. **Submit** a pull request

## 🏗️ Development Setup

### Prerequisites

- .NET 8.0 SDK (for development)
- Visual Studio 2022 / VS Code / Rider
- Git

### Building

```bash
git clone https://github.com/your-username/rpc-dotnet-toolkit.git
cd rpc-dotnet-toolkit
dotnet restore
dotnet build
```

### Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📝 Guidelines

### Code Style

- Follow C# coding conventions
- Use meaningful variable/method names
- Add XML documentation for public APIs
- Keep methods small and focused

### Commits

- Use clear, descriptive commit messages
- Reference issue numbers when applicable
- Keep commits atomic (one logical change per commit)

Example:
```
feat: Add rate limiting middleware (#123)

- Implement RateLimitMiddleware class
- Add configuration options
- Include unit tests
```

### Pull Requests

- Provide a clear description of changes
- Link to related issues
- Ensure all tests pass
- Update documentation if needed
- Add/update tests for new features

## 🧪 Testing

- Write unit tests for new features
- Maintain or improve code coverage
- Test on multiple target frameworks when possible
- Add integration tests for cross-platform scenarios

## 📚 Documentation

- Update README.md for new features
- Add XML comments to public APIs
- Create examples for complex features
- Update CHANGELOG.md

## 🐛 Reporting Issues

When reporting bugs, please include:

- **Description** - Clear description of the issue
- **Reproduction** - Steps to reproduce
- **Expected** - What you expected to happen
- **Actual** - What actually happened
- **Environment** - OS, .NET version, etc.

## 💡 Feature Requests

Feature requests are welcome! Please:

- Explain the use case
- Describe the expected behavior
- Consider if it fits the project scope
- Discuss in an issue before implementing

## 🔄 Release Process

1. Update version in `.csproj`
2. Update CHANGELOG.md
3. Create Git tag
4. GitHub Actions builds and publishes to NuGet

## 📄 License

By contributing, you agree that your contributions will be licensed under the MIT License.

## 🤝 Code of Conduct

- Be respectful and inclusive
- Welcome newcomers
- Focus on constructive feedback
- Help create a positive community

## ❓ Questions?

Feel free to open an issue for questions or discussions!

---

**Thank you for contributing to RPC .NET Toolkit!** 🙏
