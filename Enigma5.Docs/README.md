# Aenigma Technical Documentation

In-depth documentation of the Aenigma server (`Enigma5.App` and its libraries), written for
contributors and other technical readers who want to understand how the system works and why
it is built the way it is.

This documentation is an addition to the quick-start material in the repository root, not a
replacement for it:

| Document | Use it for |
|---|---|
| [`README.md`](../README.md) | Building, running, and the meaning of every `appsettings.json` setting |
| [`API.md`](../API.md) | HTTP endpoint reference |
| [`SIGNALR_API.md`](../SIGNALR_API.md) | SignalR hub method reference |
| This folder | How the system works internally, and how to change it safely |

Where a topic is already covered by one of those documents, the chapters here link to it
instead of repeating it.

## Reading order

Chapters 1 and 2 explain what Aenigma is and how it is put together, and need no prior
knowledge of the code. The remaining chapters can be read in any order; each one covers a single
subject.

1. [Introduction](01-introduction.md)
2. [Solution architecture](02-solution-architecture.md)
3. [Concurrency model](03-concurrency-model.md)
4. [Startup and runtime lifecycle](04-startup-and-lifecycle.md)
5. [Cryptography and onions](05-cryptography-and-onions.md)
6. [Keys, passphrases and the kernel keyring](06-keys-and-passphrases.md)
7. [The SignalR hub pipeline](07-hub-pipeline.md)
8. [Sessions and authentication](08-sessions-and-authentication.md)
9. [Message routing and storage](09-message-routing-and-storage.md)
10. [The network graph](10-network-graph.md)
11. [Federation: the network bridge](11-federation-bridge.md)
12. [HTTP API internals](12-http-api-internals.md)
13. [Data layer](13-data-layer.md)
14. [Access control](14-access-control.md)
15. [Configuration internals](15-configuration-internals.md)
16. [Dashboard](16-dashboard.md)
17. [Background jobs](17-background-jobs.md)
18. [Logging](18-logging.md)
19. [Security model](19-security-model.md)
20. [Known issues and technical debt](20-known-issues.md)
21. [Contributor guide](21-contributor-guide.md)

Appendix A. [Packaging, deployment and VM images (`Enigma5.Scripts`)](appendix-a-scripts.md)

## Conventions

The chapters follow the structure of an academic report and are written in an impersonal
register.

- Every chapter opens with a paragraph beginning `**Abstract.**` and closes with a section titled
  *Summary*.
- Sections are numbered hierarchically within their chapter (`4.2`, `4.2.1`), with the number
  written in the heading itself.
- Tables and listings are numbered per chapter and captioned. The caption is a paragraph placed
  directly before the table or code block, for example `**Table 4.1:** Endpoints with a defined
  role.` or `**Listing 5.1:** Byte layout of an onion layer.` Table and listing numbers are
  referenced in the text in plain form ("Table 4.1").
- Protocols and data formats are specified in listings with a compact notation; `‖` denotes
  concatenation, and each listing defines its remaining symbols.
- Paths are relative to the repository root, for example `Enigma5.App/Hubs/RoutingHub.cs`.
- "Node" denotes one running instance of `Enigma5.App`; "client" denotes an end-user application
  (currently the Aenigma Android app) that connects to a node.
- Examples use the default ports: `8080` for the public endpoint and `8081` for the control
  endpoint.

The PDF build (below) turns the caption and abstract paragraphs into proper captions and abstract
blocks; on GitHub they appear as ordinary bold-prefixed paragraphs.

## Building the PDF

The chapters are plain Markdown and can be read directly on GitHub or in an editor. A single
PDF containing all chapters can be generated with [pandoc](https://pandoc.org/) and
[WeasyPrint](https://weasyprint.org/):

```bash
sudo apt-get install pandoc weasyprint
./Enigma5.Docs/build/build-pdf.sh
```

The PDF is written to `Enigma5.Docs/dist/aenigma-technical-documentation.pdf`. The `dist`
folder is not tracked by git.
