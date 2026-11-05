# Reflection

Missing authentication/authorization.
App settings not set up. with base urls and so on

# DB
Implement Transactions in the CR(UD) operations.

# LogService
THe intention here was to create a standalone service that would handle the logging at a common place. Would like to have had it event driven with this service as the sole consumer initially. while the Aggregation service and  n + services would be multiple producers of a Log event via message broker.

Changes that would be neccesary if setting this up as intended is to alter the dedupe logic.


# KYC Aggregation service

Resilliance should be added against the consumed services.
Partial failure where some data can be fetched and some isnt are not handled at all.
There is some summarization 

When is the persisted data considerd stale?

Decided to store json payload for changes in schema from consumed services. GDPR sensitive and needs a Cleanup step to clear sensetive data.


# Unittests
One test should test one and only one functionality

Did not spend supermuch time on this part to be honest.

