# Vector Architecture Diagrams

This directory contains architecture diagrams for the Vector system in multiple formats.

## Diagram Files

| File | Format | Description |
|------|--------|-------------|
| `business-layer.puml` | PlantUML | ArchiMate 3.2 Business Layer |
| `application-layer.puml` | PlantUML | ArchiMate 3.2 Application Layer |
| `technology-layer.puml` | PlantUML | ArchiMate 3.2 Technology Layer |
| `full-architecture.puml` | PlantUML | Complete ArchiMate 3.2 Model |
| `vector-archimate.xml` | ArchiMate Open Exchange | LeanIX/Archi Import Format |
| `architecture-overview.mmd` | Mermaid | High-level system architecture |
| `domain-model.mmd` | Mermaid | Domain-Driven Design class diagram |
| `submission-workflow.mmd` | Mermaid | End-to-end sequence diagram |
| `c4-context.mmd` | Mermaid | C4 System Context diagram |

## Generating PNG Images

### Option 1: VS Code Extensions

Install these VS Code extensions:
- **PlantUML** (`jebbs.plantuml`) - For `.puml` files
- **Mermaid Markdown Syntax Highlighting** (`bpruitt-goddard.mermaid-markdown-syntax-highlighting`)
- **Markdown Preview Mermaid Support** (`bierner.markdown-mermaid`)

Right-click on a `.puml` file and select "Export Current Diagram" to generate PNG.

### Option 2: PlantUML CLI

```bash
# Install PlantUML (requires Java)
# Download from https://plantuml.com/download

# Generate PNG from PlantUML files
java -jar plantuml.jar *.puml

# Generate SVG
java -jar plantuml.jar -tsvg *.puml
```

### Option 3: Mermaid CLI

```bash
# Install mermaid-cli
npm install -g @mermaid-js/mermaid-cli

# Generate PNG from Mermaid files
mmdc -i architecture-overview.mmd -o architecture-overview.png
mmdc -i domain-model.mmd -o domain-model.png
mmdc -i submission-workflow.mmd -o submission-workflow.png
mmdc -i c4-context.mmd -o c4-context.png

# Generate all at once
for f in *.mmd; do mmdc -i "$f" -o "${f%.mmd}.png"; done
```

### Option 4: Online Tools

- **PlantUML**: https://www.plantuml.com/plantuml/uml/
- **Mermaid**: https://mermaid.live/

## LeanIX Import

The `vector-archimate.xml` file is in ArchiMate 3.0 Open Exchange Format, which can be imported into:

1. **LeanIX**
   - Go to Administration > Import
   - Select "ArchiMate" as the import type
   - Upload `vector-archimate.xml`
   - Map elements to LeanIX fact sheets

2. **Archi (Open Source)**
   - Download from https://www.archimatetool.com/
   - File > Import > Open Exchange XML Model...
   - Select `vector-archimate.xml`
   - Export as PNG: File > Export > Export View as Image

3. **BiZZdesign Enterprise Studio**
   - File > Import > ArchiMate Model Exchange File
   - Select `vector-archimate.xml`

## ArchiMate 3.2 Element Types Used

### Motivation Layer
- Stakeholder, Driver, Goal, Requirement

### Business Layer
- Business Actor, Business Process, Business Service, Business Object

### Application Layer
- Application Component, Application Service, Application Function
- Data Object, Application Interface

### Technology Layer
- Node, Device, System Software, Artifact
- Communication Network, Technology Service

## Diagram Conventions

- **Colors**: Follow ArchiMate standard layer colors
- **Relationships**: Use ArchiMate relationship types
- **Naming**: Use clear, business-readable names
- **Documentation**: Include descriptions in model elements

## Updating Diagrams

When updating the architecture:

1. Modify the appropriate `.puml` or `.mmd` file
2. Regenerate PNG images
3. Update `vector-archimate.xml` if structural changes
4. Commit all changes together
