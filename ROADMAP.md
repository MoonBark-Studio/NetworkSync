# MoonBark NetworkSync Roadmap

## Short-term
* [x] Core layer decoupling and folder scaffolding
* [x] Standard C# namespace alignment
* [x] Verification of full test coverage

## Long-term
* [ ] Advanced optimization under heavy simulation loads
* [ ] Rich editor integrations and custom inspectors

## Commercial Independence (pre-release gate)

> Portfolio-wide principle + debt: [`../COMMERCIAL-INDEPENDENCE.md`](../COMMERCIAL-INDEPENDENCE.md).

**If this plugin is released/sold standalone** (Godot Asset Library, itch.io), it must be **commercially independent** — install and work with no buyer-facing dependency on `MoonBark.Framework`. Internal `Core/ECS/Godot` layering is fine; an external dependency the buyer must acquire is not.

- [ ] **Audit framework coupling** — list every `MoonBark.Framework` type/service consumed at the Core/ECS/Godot layers.
- [ ] **Flip the dependency arrow** — replace each with a plugin-owned interface (dependency inversion) or a vendored type, so the relationship is `Framework → Plugin`, never `Plugin → Framework`.
- [ ] **Single drop-in package** — buyer installs one unit; internal layering stays invisible (no layer-cake of DLLs / no required external refs).
- [ ] **GDScript-first** — most Godot buyers are GDScript and expect a drag-into-`addons/` install; ship the GDScript drop-in first. C# (if kept) is repackaged to this same standard before sale, otherwise it stays an internal parity track.
