# PROPGRID Changelog

## v1.0.1 — 2026-05-31

### Bug Fix: `SelectedPropertyChanged` event always fired with `PropertyName = null`

Fixed five related bugs in `WpfPropertyGrid.cs` that prevented the
`SelectedPropertyChanged` event from reporting the focused property name:
**`CoerceSelectedObjects` read the wrong property** — read `SelectedObjectsProperty`
   instead of `SelectedObjectProperty`, so the null-check always took the wrong branch.
**Returned stale value** — returned the existing empty array instead of wrapping the
   resolved single object in a new `object[]`.
**Infinite recursion** — `SelectedObjectsPropertyChanged` re-coerced itself on entry;
   each coercion produced a new array reference, causing a `StackOverflowException`.
**Duplicate notifications** — `SelectedObjectPropertyChanged` invoked
   `OnSelectionChanged` both directly and via coercion. Removed the direct call.
**Wrong type label** — `<multiple>` was appended even for single-object selections.

---

## v1.0.0 — 2026-05-31

Initial release.
