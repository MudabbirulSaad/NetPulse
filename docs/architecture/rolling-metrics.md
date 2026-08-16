# Rolling latency metrics

The selected-target panel calculates minimum, average, and maximum latency from the retained
service responses. Healthy and degraded responses count because the monitored service replied;
offline and error results do not receive latency values.

A small built-in WPF element renders the latest 30 results without a chart package. It scales
to the available panel, draws three quiet guide lines, and breaks the line whenever a result has
no measurable service response. The graph therefore communicates outages as gaps instead of
misleading zero-millisecond points.
