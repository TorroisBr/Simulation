# Definition-binding design attached to the member ledger

Architecture source: 512cf5721927a5b3d6766bc4d19cfd62a6729a41.

- Selected owner and shared graph boundary: docs/architecture/CORE_F_DEFINITION_BINDING_TECHNICAL_DESIGN.md (blob 7be12e59aadd6a211f731fd52aa636fa073bb2b5).
- Selected site and optional local descriptors: docs/architecture/CORE_F_OPTIONAL_DEFINITION_BINDING_DESIGN.md (blob 99dfea080462c4849e01530535903fbc609acdf7).

split-host-destination-groups.csv is the source-linked member ledger: each grouped caller/target row retains its concrete source/member, exact current target signature, declaration sites, destination, and links to every original compiler row by ordinal and input-row hash. Read the two design documents above for the approved future constructor/accessor signatures and immutable definition-node bindings. This qualification records current ownership and access destinations only; it does not implement those definition APIs, create stubs for empty selected content, or mark the Shared Runtime/Application/Host ready for implementation.
