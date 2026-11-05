$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$outputPath = Join-Path $scriptRoot 'KYCAggrService.PostmanCollection.json'

$kycBaseUrl = 'http://localhost:5023'
$loggingBaseUrl = 'http://localhost:5201'
$validSsn = '19800115-1234'
$invalidSsn = 'invalid-ssn'
$notFoundSsn = '99999999-9999'

$collection = [ordered]@{
    info = [ordered]@{
        name = 'KYCAggrService Local API Collection'
        description = 'Prepared Postman requests and assertions for the KYC aggregation service and logging microservice.'
        schema = 'https://schema.getpostman.com/json/collection/v2.1.0/collection.json'
        version = '1.0.0'
    }
    item = @(
        [ordered]@{
            name = 'KYC Aggregation API'
            item = @(
                [ordered]@{
                    name = 'Health check'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/health"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('health')
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Service is healthy", function () { var json = pm.response.json(); pm.expect(json.status).to.eql("ok"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Get aggregated KYC data'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/kyc-data/$validSsn"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('kyc-data', $validSsn)
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Response contains SSN", function () { var json = pm.response.json(); pm.expect(json.ssn).to.eql("19800115-1234"); });',
                                    'pm.test("Summary response includes identity fields", function () { var json = pm.response.json(); pm.expect(json.firstName).to.be.a("string"); pm.expect(json.fetchState).to.be.a("string"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Get full aggregated KYC data'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/kyc-data/$validSsn/full"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('kyc-data', $validSsn, 'full')
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Full response contains personal and contact data", function () { var json = pm.response.json(); pm.expect(json.ssn).to.eql("19800115-1234"); pm.expect(json.personalDetails).to.be.an("object"); pm.expect(json.contactDetails).to.be.an("object"); pm.expect(json.kycForm).to.be.an("object"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Invalid SSN request'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/kyc-data/$invalidSsn"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('kyc-data', $invalidSsn)
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 400", function () { pm.response.to.have.status(400); });',
                                    'pm.test("Validation error is returned", function () { var json = pm.response.json(); pm.expect(json.error).to.be.a("string"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Full data invalid SSN request'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/kyc-data/$invalidSsn/full"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('kyc-data', $invalidSsn, 'full')
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 400", function () { pm.response.to.have.status(400); });',
                                    'pm.test("Validation error is returned", function () { var json = pm.response.json(); pm.expect(json.error).to.be.a("string"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Customer not found'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$kycBaseUrl/kyc-data/$notFoundSsn"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5023'
                            path = @('kyc-data', $notFoundSsn)
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 404", function () { pm.response.to.have.status(404); });',
                                    'pm.test("Not found error is returned", function () { var json = pm.response.json(); pm.expect(json.error).to.be.a("string"); });'
                                )
                            }
                        }
                    )
                }
            )
        },
        [ordered]@{
            name = 'Logging Service API'
            item = @(
                [ordered]@{
                    name = 'Health check'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$loggingBaseUrl/health"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5201'
                            path = @('health')
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Service is healthy", function () { var json = pm.response.json(); pm.expect(json.status).to.eql("ok"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Get recent logs'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$loggingBaseUrl/api/logs?limit=20"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5201'
                            path = @('api', 'logs')
                            query = @(
                                [ordered]@{ key = 'limit'; value = '20' }
                            )
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Response is an array", function () { pm.expect(pm.response.json()).to.be.an("array"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Get logs by transaction ID'
                    request = [ordered]@{
                        method = 'GET'
                        header = @()
                        url = [ordered]@{
                            raw = "$loggingBaseUrl/api/logs?transactionId=postman-correlation-001"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5201'
                            path = @('api', 'logs')
                            query = @(
                                [ordered]@{ key = 'transactionId'; value = 'postman-correlation-001' }
                            )
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Transaction logs are returned", function () { var json = pm.response.json(); pm.expect(json).to.be.an("array"); pm.expect(json.length).to.be.greaterThan(0); });',
                                    'pm.test("Matching correlation ID is returned", function () { var json = pm.response.json(); var ids = json.map(function (entry) { return entry.correlationId; }); pm.expect(ids).to.include("postman-correlation-001"); });'
                                )
                            }
                        }
                    )
                },
                [ordered]@{
                    name = 'Persist a log event'
                    request = [ordered]@{
                        method = 'POST'
                        header = @(
                            [ordered]@{ key = 'Content-Type'; value = 'application/json' }
                        )
                        body = [ordered]@{
                            mode = 'raw'
                            raw = '{
  "eventName": "KycRequestReceived",
  "serviceName": "KycAggregation.Api",
  "level": "Information",
  "message": "KYC request received for manual Postman validation.",
  "correlationId": "postman-correlation-001",
  "occurredUtc": "2026-11-05T12:00:00Z",
  "context": {
    "ssn": "19800115-1234",
    "source": "postman"
  }
}'
                            options = [ordered]@{
                                raw = [ordered]@{
                                    language = 'json'
                                }
                            }
                        }
                        url = [ordered]@{
                            raw = "$loggingBaseUrl/api/logs"
                            protocol = 'http'
                            host = @('localhost')
                            port = '5201'
                            path = @('api', 'logs')
                        }
                    }
                    response = @()
                    event = @(
                        [ordered]@{
                            listen = 'test'
                            script = [ordered]@{
                                type = 'text/javascript'
                                exec = @(
                                    'pm.test("Status code is 200", function () { pm.response.to.have.status(200); });',
                                    'pm.test("Log accepted successfully", function () { var json = pm.response.json(); pm.expect(json.success).to.eql(true); });',
                                    'pm.test("Returned log id exists", function () { var json = pm.response.json(); pm.expect(json.id).to.be.a("string"); });'
                                )
                            }
                        }
                    )
                }
            )
        }
    )
}

$collectionJson = $collection | ConvertTo-Json -Depth 20
$collectionJson | Set-Content -Path $outputPath -Encoding utf8

Write-Host "Postman collection generated: $outputPath"
