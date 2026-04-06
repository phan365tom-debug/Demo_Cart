# Debugging Evidence (Required)

This document includes 2 intentional failures and fixes.

## Failure 1: localhost DB bug

### Intentional break

Set DB_HOST in k8s/01-configmap-api.yaml to localhost and re-apply:

```powershell
kubectl apply -f k8s/01-configmap-api.yaml
kubectl rollout restart deploy/api-deploy -n demo
```

### Symptom

- API readiness fails
- Pod stays NotReady
- Logs show SQL connection failure/timeouts

Commands:

```powershell
kubectl -n demo get pods
kubectl -n demo describe pod <api-pod-name>
kubectl -n demo logs deploy/api-deploy
```

### Root cause

Inside Kubernetes, localhost refers to the container itself, not host machine SQL Server.

### Fix

Change DB_HOST back to host.minikube.internal and restart deployment.

---

## Failure 2: wrong password secret bug

### Intentional break

Set DB_PASSWORD in k8s/02-secret-db.yaml to invalid value, apply, and restart API.

### Symptom

- API process starts but readiness fails
- Logs show login failed for user or authentication failure

Commands:

```powershell
kubectl -n demo get pods
kubectl -n demo logs deploy/api-deploy
kubectl -n demo describe pod <api-pod-name>
```

### Root cause

Kubernetes Secret value does not match SQL Server credential.

### Fix

Restore valid DB_PASSWORD and restart API deployment.

---

## 3-Layer Playbook Applied

1. Kubernetes layer: pod status and events
2. Application layer: deployment logs
3. Dependency layer: SQL server auth and network reachability
