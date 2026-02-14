# Vector Operations Guide

## Table of Contents

1. [Deployment](#deployment)
2. [Configuration](#configuration)
3. [Monitoring](#monitoring)
4. [Troubleshooting](#troubleshooting)
5. [Maintenance](#maintenance)
6. [Disaster Recovery](#disaster-recovery)

---

## Deployment

### Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed and authenticated
- kubectl configured for AKS access
- Helm 3.x installed

### Infrastructure Provisioning

```bash
# Create resource group
az group create --name rg-vector-prod --location eastus

# Create AKS cluster
az aks create \
  --resource-group rg-vector-prod \
  --name aks-vector-prod \
  --node-count 3 \
  --enable-managed-identity \
  --generate-ssh-keys

# Create SQL Database
az sql server create \
  --name sql-vector-prod \
  --resource-group rg-vector-prod \
  --admin-user vectoradmin \
  --admin-password <SecurePassword>

az sql db create \
  --resource-group rg-vector-prod \
  --server sql-vector-prod \
  --name VectorDb \
  --service-objective S3

# Create Redis Cache
az redis create \
  --resource-group rg-vector-prod \
  --name redis-vector-prod \
  --sku Standard \
  --vm-size c1

# Create Storage Account
az storage account create \
  --resource-group rg-vector-prod \
  --name stvectorprod \
  --sku Standard_LRS

# Create Service Bus
az servicebus namespace create \
  --resource-group rg-vector-prod \
  --name sb-vector-prod \
  --sku Standard
```

### Application Deployment

```bash
# Get AKS credentials
az aks get-credentials --resource-group rg-vector-prod --name aks-vector-prod

# Create namespace
kubectl create namespace vector

# Apply secrets
kubectl apply -f kubernetes/secrets.yaml -n vector

# Deploy using Helm
helm upgrade --install vector ./helm/vector \
  --namespace vector \
  --values ./helm/vector/values-prod.yaml
```

### CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) handles:

1. **Build**: Compile and run tests
2. **Security Scan**: Dependency vulnerability check
3. **Docker Build**: Build and push container images
4. **Deploy Staging**: Automatic deployment to staging
5. **Deploy Production**: Manual approval required

---

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | Yes |
| `ConnectionStrings__Redis` | Redis connection string | Yes |
| `ConnectionStrings__BlobStorage` | Blob storage connection string | Yes |
| `MessageBus__ConnectionString` | Service Bus connection string | Yes |
| `DocumentIntelligence__Endpoint` | Azure Doc Intelligence endpoint | Yes |
| `DocumentIntelligence__ApiKey` | Azure Doc Intelligence key | Yes |
| `EmailService__TenantId` | Azure AD tenant ID | Yes |
| `EmailService__ClientId` | Azure AD app client ID | Yes |
| `EmailService__ClientSecret` | Azure AD app client secret | Yes |
| `EmailService__SharedMailbox` | Email address to monitor | Yes |

### Configuration Files

#### appsettings.Production.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "ApplicationInsights" }
    ]
  },
  "UseInMemoryDatabase": false,
  "UseMockServices": false,
  "SeedDatabase": false,
  "EmailService": {
    "Provider": "Graph",
    "PollingIntervalSeconds": 30
  },
  "MessageBus": {
    "Provider": "AzureServiceBus"
  },
  "DocumentIntelligence": {
    "Provider": "Azure"
  }
}
```

### Kubernetes ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: vector-config
  namespace: vector
data:
  ASPNETCORE_ENVIRONMENT: "Production"
  EmailService__PollingIntervalSeconds: "30"
  Logging__LogLevel__Default: "Information"
```

### Kubernetes Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: vector-secrets
  namespace: vector
type: Opaque
stringData:
  ConnectionStrings__DefaultConnection: "<encrypted>"
  ConnectionStrings__Redis: "<encrypted>"
  ConnectionStrings__BlobStorage: "<encrypted>"
```

---

## Monitoring

### Health Endpoints

| Endpoint | Purpose | Expected Response |
|----------|---------|-------------------|
| `/health` | Overall system health | `{"status":"Healthy"}` |
| `/health/live` | Kubernetes liveness | HTTP 200 |
| `/health/ready` | Kubernetes readiness | HTTP 200 |

### Kubernetes Probes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 10
  failureThreshold: 3

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 3
```

### Application Insights Metrics

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| Request Duration | API response time | > 500ms (p95) |
| Request Rate | Requests per second | Baseline ± 50% |
| Failed Requests | HTTP 5xx errors | > 1% |
| Dependency Duration | External call latency | > 1000ms |
| Exception Rate | Unhandled exceptions | > 0 |

### Key Performance Indicators (KPIs)

| KPI | Target | Measurement |
|-----|--------|-------------|
| Email-to-Submission Time | < 30 seconds | Time from email receipt to submission creation |
| API Response Time (p95) | < 500ms | 95th percentile of API requests |
| Document Processing Success | > 95% | Successful extractions / total documents |
| System Availability | 99.9% | Uptime / total time |

### Log Queries (Application Insights / KQL)

```kusto
// Failed requests
requests
| where success == false
| summarize count() by bin(timestamp, 1h), operation_Name
| order by timestamp desc

// Slow dependencies
dependencies
| where duration > 1000
| summarize avg(duration), count() by target, name
| order by avg_duration desc

// Document processing errors
traces
| where message contains "Document processing failed"
| summarize count() by bin(timestamp, 1h)
```

### Alerts Configuration

| Alert | Condition | Action |
|-------|-----------|--------|
| High Error Rate | > 5% HTTP 5xx in 5 min | Page on-call |
| Database Connection Failed | Health check failure | Page on-call |
| Email Polling Stopped | No emails processed in 1 hour | Notify team |
| High Memory Usage | > 85% pod memory | Scale up |

---

## Troubleshooting

### Common Issues

#### Issue: Emails Not Being Processed

**Symptoms**: No new submissions appearing, email polling logs show no activity

**Investigation**:
```bash
# Check worker pod logs
kubectl logs -l app=vector-worker -n vector --tail=100

# Check email service health
kubectl exec -it deployment/vector-api -n vector -- curl localhost:8080/health
```

**Possible Causes**:
1. Microsoft Graph API credentials expired
2. Shared mailbox permissions revoked
3. Service Bus connection failure
4. Worker pod not running

**Resolution**:
1. Rotate client secret in Azure AD and update Kubernetes secret
2. Verify mailbox permissions in Azure AD
3. Check Service Bus namespace health in Azure portal
4. Restart worker deployment: `kubectl rollout restart deployment/vector-worker -n vector`

#### Issue: High API Latency

**Symptoms**: API response times > 500ms, user complaints about slow UI

**Investigation**:
```kusto
// Find slow endpoints
requests
| where duration > 500
| summarize count(), avg(duration) by operation_Name
| order by count_ desc
```

**Possible Causes**:
1. Database query performance
2. Redis cache misses
3. High pod resource usage

**Resolution**:
1. Review SQL query execution plans, add indexes
2. Check Redis connection and cache hit ratio
3. Scale up pods or add more replicas

#### Issue: Document Extraction Failures

**Symptoms**: Documents showing "Processing Failed" status, low confidence scores

**Investigation**:
```bash
# Check processing job details
curl -H "X-Tenant-Id: {tenantId}" \
  https://api.vector.example.com/api/v1/document-processing/jobs/{jobId}
```

**Possible Causes**:
1. Document format not supported
2. Azure Document Intelligence quota exceeded
3. Low quality scans

**Resolution**:
1. Check supported document types, request format conversion
2. Increase Azure Document Intelligence quota
3. Request higher quality scans from producer

### Debug Commands

```bash
# Get pod status
kubectl get pods -n vector -o wide

# Describe pod for events
kubectl describe pod <pod-name> -n vector

# Get pod logs
kubectl logs <pod-name> -n vector --tail=200

# Execute into pod
kubectl exec -it <pod-name> -n vector -- /bin/sh

# Check resource usage
kubectl top pods -n vector

# Port forward for local debugging
kubectl port-forward deployment/vector-api 5000:8080 -n vector
```

---

## Maintenance

### Database Maintenance

#### Index Maintenance (Weekly)

```sql
-- Rebuild fragmented indexes
ALTER INDEX ALL ON Submissions REBUILD;
ALTER INDEX ALL ON Coverages REBUILD;
ALTER INDEX ALL ON ExposureLocations REBUILD;
ALTER INDEX ALL ON ProcessingJobs REBUILD;

-- Update statistics
EXEC sp_updatestats;
```

#### Data Archival (Monthly)

```sql
-- Archive old processing jobs (> 90 days)
INSERT INTO ProcessingJobsArchive
SELECT * FROM ProcessingJobs
WHERE CreatedAt < DATEADD(day, -90, GETDATE())
AND Status = 'Completed';

DELETE FROM ProcessingJobs
WHERE CreatedAt < DATEADD(day, -90, GETDATE())
AND Status = 'Completed';
```

### Certificate Rotation

1. Generate new certificates 30 days before expiry
2. Update Kubernetes secrets
3. Perform rolling restart of deployments

```bash
# Update TLS secret
kubectl create secret tls vector-tls \
  --cert=new-cert.pem \
  --key=new-key.pem \
  --dry-run=client -o yaml | kubectl apply -f -

# Rolling restart
kubectl rollout restart deployment/vector-api -n vector
```

### Dependency Updates

1. Review Dependabot PRs weekly
2. Test updates in staging environment
3. Deploy to production after validation

---

## Disaster Recovery

### Backup Strategy

| Component | Backup Method | Frequency | Retention |
|-----------|---------------|-----------|-----------|
| SQL Database | Azure Backup | Daily | 30 days |
| Blob Storage | Geo-redundant | Continuous | N/A |
| Configuration | Git | Continuous | N/A |
| Secrets | Key Vault | Versioned | 90 days |

### Recovery Procedures

#### Database Recovery

```bash
# Restore from point-in-time
az sql db restore \
  --dest-name VectorDb-Restored \
  --edition Standard \
  --service-objective S3 \
  --resource-group rg-vector-prod \
  --server sql-vector-prod \
  --source-database VectorDb \
  --time "2024-01-15T10:00:00Z"
```

#### Full Environment Recovery

1. Provision infrastructure using Terraform/Bicep
2. Restore database from backup
3. Deploy application using Helm
4. Verify health checks
5. Update DNS to point to new environment

### Recovery Time Objectives

| Scenario | RTO | RPO |
|----------|-----|-----|
| Single pod failure | < 1 minute | 0 |
| Node failure | < 5 minutes | 0 |
| Region failure | < 4 hours | < 1 hour |
| Database corruption | < 2 hours | < 24 hours |

### Business Continuity

1. **Primary Region**: East US
2. **DR Region**: West US 2
3. **Failover**: Manual (automated for critical services)
4. **DNS Failover**: Azure Traffic Manager
