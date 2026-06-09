# NetBridge

NetBridge is a C# desktop application and library that provides network proxy bridging functionality for Windows. It wraps the native capabilities of [ProxyBridge](https://github.com/InterceptSuite/ProxyBridge) to redirect TCP and UDP traffic through HTTP/SOCKS5 proxies.

---

## Features

- Redirect TCP and UDP network traffic through HTTP or SOCKS5 proxies
- Cross-process traffic interception via ProxyBridge native DLL
- Desktop GUI (`NetBridge.Desktop`) and reusable library (`NetBridgeLib`)
- Built with C# / .NET

---

## Getting Started

### Prerequisites

- Windows OS
- .NET 8 or later
- The ProxyBridge native DLL (included or obtained from [InterceptSuite/ProxyBridge](https://github.com/InterceptSuite/ProxyBridge))

### Build

1. Clone this repository:
   ```bash
   git clone https://github.com/2dust/NetBridge.git
   cd NetBridge
   ```

2. Open the solution:
   ```
   src/NetBridge.slnx
   ```

3. Build with Visual Studio 2022 or the .NET CLI:
   ```bash
   dotnet build src/NetBridge.slnx
   ```

---

## Project Structure

```
src/
├── NetBridge.Desktop/     # Desktop GUI application
├── NetBridgeLib/          # Core library (proxy bridge wrapper)
├── NetBridge.slnx         # Solution file
├── Directory.Build.props
└── Directory.Packages.props
```

---

## Third-Party Components

This project uses the following third-party component:

### ProxyBridge

- **Source:** [https://github.com/InterceptSuite/ProxyBridge](https://github.com/InterceptSuite/ProxyBridge)
- **Author:** [Anof-cyber / InterceptSuite](https://github.com/InterceptSuite)
- **Description:** A Proxifier alternative that redirects Windows/macOS/Linux TCP and UDP traffic to HTTP/SOCKS5 proxies.
- **License:** MIT License

```
MIT License

Copyright (c) 2025 Anof-cyber/InterceptSuite

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## License

NetBridge is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

See [LICENSE](./LICENSE) for the full license text.

> **Note on third-party licenses:** The ProxyBridge native DLL used by this project is distributed under the MIT License, which is compatible with GPL-3.0. The MIT license notice above must be preserved in all distributions of this software that include the ProxyBridge component.

---

## Acknowledgements

- [InterceptSuite/ProxyBridge](https://github.com/InterceptSuite/ProxyBridge) — for the excellent open-source proxy redirection engine that powers this project.

---

## Contributing

Issues and pull requests are welcome. Please open an issue first to discuss what you would like to change.
