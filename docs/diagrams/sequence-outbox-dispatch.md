# sequence-outbox-dispatch

`mermaid
sequenceDiagram
  participant S as Service
  participant O as Outbox Store
  participant D as Dispatcher
  S->>O: add pending messages
  D->>O: mark processing
  D->>O: publish or fail
  O-->>D: status + retry evidence
`
