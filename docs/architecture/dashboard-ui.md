# Dashboard UI

The single-window WPF shell uses an industrial signal-console visual language. A compact target
rail supports rapid scanning, while one focus panel shows the selected target's latest service
result and independent ICMP information. The layout remains usable down to 980 by 650 pixels.

Summary cards are calculated only from live session snapshots: target count, healthy count,
attention count, and the average of current measurable response times. Failed checks do not
receive invented latency values.

Health is never communicated by colour alone. Every state combines a stable label, glyph, and
colour, and command buttons provide automation names for assistive technology. The ViewModel
depends only on `INetPulseSession`; the application startup is the sole composition point that
references the infrastructure factory.
