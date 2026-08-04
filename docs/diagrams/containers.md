# containers

`mermaid
graph TD
  Clean[Clean API] --> Shared[Shared Kernel]
  Vertical[Vertical Slice API] --> Shared
  Modular[Modular Host] --> Shared
  Hex[Hexagonal API] --> Shared
  CLI[archtool] --> Shared
`
