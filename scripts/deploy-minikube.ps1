$ErrorActionPreference = "Stop"

Write-Host "Setting Minikube Docker environment..."
minikube -p minikube docker-env | Invoke-Expression

Write-Host "Publishing API binaries..."
dotnet publish ./Api/Api.csproj -c Release -o ./Api/publish /p:UseAppHost=false

Write-Host "Building UI static bundle..."
Push-Location ./ui
npm install
npm run build
Pop-Location

Write-Host "Building API image..."
docker build -t kubecart-api:latest ./Api

Write-Host "Building UI image..."
docker build -t kubecart-ui:latest ./ui

Write-Host "Enabling ingress addon..."
minikube addons enable ingress

Write-Host "Applying Kubernetes manifests..."
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap-api.yaml
kubectl apply -f k8s/02-secret-db.yaml
kubectl apply -f k8s/03-secret-jwt.yaml
kubectl apply -f k8s/09-secret-ai.yaml
kubectl apply -f k8s/04-deployment-api.yaml
kubectl apply -f k8s/05-service-api.yaml
kubectl apply -f k8s/06-deployment-ui.yaml
kubectl apply -f k8s/07-service-ui.yaml
kubectl apply -f k8s/08-ingress.yaml

Write-Host "Waiting for rollouts..."
kubectl rollout status deploy/api-deploy -n demo --timeout=180s
kubectl rollout status deploy/ui-deploy -n demo --timeout=180s

Write-Host "Deployment complete."
Write-Host "Run in separate terminal: minikube tunnel"
Write-Host "Then open: http://kubecart.local"
