# Worker implementation notes

`RxFlow.Workers` owns Hangfire job handlers and, unless a later decision changes it, Kafka consumers. Jobs reload authoritative order state, perform idempotent transitions, call application/infrastructure ports, and publish status events. Hangfire owns bounded retries; handlers must be safe under duplicate delivery and must not use timing sleeps for coordination.
