# DNS and ICMP probes

DNS targets send an explicit A-record query to the selected IPv4 or IPv6 resolver. The
adapter disables the DnsClient cache and automatic retries, making each recorded sample one
observable request. The target timeout is also a hard upper bound for the combined operation.

Service-health mapping distinguishes responses from transport failures:

- A response containing at least one A record is healthy.
- NXDOMAIN, refused, server failure, or an empty successful response is degraded.
- Resolver timeout or socket failure is offline.
- Invalid configuration or an unexpected application failure is an error.

An ICMP echo to the selected resolver starts alongside the DNS request. Its typed result is
shown as supporting reachability information only. Blocked or failed ICMP never overrides a
successful DNS response.
