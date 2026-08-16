# Target management

The target editor uses the same core validator as the session. It exposes only HTTP and DNS
A-record modes, the four supported polling intervals, a bounded timeout, and an enabled flag.
DNS mode reveals the explicit resolver IP field; HTTP mode accepts only absolute HTTP or HTTPS
addresses. Validation errors stay next to the relevant fields.

Test Connection runs through `INetPulseSession`, sharing the existing target's exclusion gate
when its endpoint matches. Saving performs a current connection test. An offline or error result
opens a clear save-anyway warning; a degraded result does not, because the remote service or DNS
resolver responded.

The dashboard exposes add, edit, test, enable/disable, and delete actions for the selected row.
Deletion requires confirmation and removes the target's local history. Default targets use the
same commands and have no special protection, so they remain fully removable.
