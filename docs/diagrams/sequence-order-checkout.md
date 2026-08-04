# sequence-order-checkout

`mermaid
sequenceDiagram
  participant C as Client
  participant A as API/CLI
  participant S as CommerceService
  participant O as Outbox
  C->>A: checkout
  A->>S: create customer/order/item/confirm/payment
  S->>O: persist domain events
  S-->>A: scenario report
  A-->>C: state + outbox evidence
`
