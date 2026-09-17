# UI operation states

Lifecycle shared by the three tabs. `OperationCoordinator` reserves the operation slot before the
first `await`, so the Running state ignores a second click and no two operations ever run at once.

```mermaid
stateDiagram-v2
    [*] --> Empty
    Empty --> Ready: valid input selected
    Ready --> Ready: change input, ResetResult
    Ready --> Running: action clicked, Operations.Begin
    Running --> Running: second action ignored while busy
    Running --> ConfirmCancel: Cancel clicked
    ConfirmCancel --> Running: "No, continue"
    ConfirmCancel --> Cancelled: "Yes, cancel", token.Cancel
    Running --> Success: OperationResult.Ok
    Running --> Cancelled: ErrorCode.Cancelled, neutral notice
    Running --> Failed: another ErrorCode, localized
    Success --> Ready: change input
    Cancelled --> Ready: change input
    Failed --> Ready: change input
    Success --> [*]: component disposed
    Cancelled --> [*]: component disposed
    Failed --> [*]: component disposed
```

Notes:

- Success, cancellation and error visuals are consistent across the three tabs, and a failure always
  clears the progress bar.
- The action stays disabled until the input is valid: a source file and a different target format
  for the audio and extract tabs, and a well-formed URL plus a destination folder for the download
  tab.
- The inline cancel confirmation disappears on its own if the operation finishes first.
