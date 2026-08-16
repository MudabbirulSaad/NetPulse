# Monitoring session

The WPF application depends on one deep interface, `INetPulseSession`. It exposes immutable
state snapshots and commands for initialization, lifecycle control, one-off checks, and target
changes. Probe selection, task coordination, cancellation, and persistence stay behind this
module boundary.

Each enabled target owns one sequential polling loop. A per-target semaphore is shared by the
polling loop and Test Connection, so the same target cannot be checked concurrently. Different
targets run independently; an exception in one adapter becomes a typed error result and does
not stop any other loop.

Application shutdown cancels the root token and awaits every active loop. Caller cancellation
propagates through the adapters and restores the target's previous visible state, so shutdown
does not create an artificial offline result.

Target changes are serialized with lifecycle operations. Updating or disabling a target stops
its loop before mutation, while enabling or adding a target starts a loop when the session is
already active.
