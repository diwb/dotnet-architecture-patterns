# components

`mermaid
graph TD
  Service[CommerceService] --> Repositories
  Service --> Outbox
  Service --> Audit
  Order --> Events[Domain Events]
`
