$ErrorActionPreference = "Stop"

if (-not (Get-Command helm -ErrorAction SilentlyContinue)) {
    Write-Host "Helm is required. Install Helm first: https://helm.sh/docs/intro/install/"
    exit 1
}

Write-Host "Creating monitoring namespace..."
kubectl create namespace monitoring --dry-run=client -o yaml | kubectl apply -f -

Write-Host "Adding Prometheus community Helm repo..."
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts
helm repo update

Write-Host "Installing kube-prometheus-stack (includes Grafana)..."
helm upgrade --install kube-prom-stack prometheus-community/kube-prometheus-stack `
  --namespace monitoring `
  --set grafana.adminPassword=admin123 `
  --set grafana.service.type=ClusterIP

Write-Host "Waiting for Grafana deployment rollout..."
kubectl rollout status deploy/kube-prom-stack-grafana -n monitoring --timeout=300s

Write-Host "Port forwarding Grafana on http://localhost:3000 ..."
Write-Host "Username: admin"
Write-Host "Password: admin123"
kubectl port-forward svc/kube-prom-stack-grafana -n monitoring 3000:80
