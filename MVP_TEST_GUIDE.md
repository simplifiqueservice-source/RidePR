# RidePR MVP Test Guide

Guia rapido para testar o fluxo minimo de corrida pelo Swagger.

## 1. Subir dependencias

No diretorio raiz do repositorio:

```powershell
docker compose up -d postgres redis
```

Execute a API pelo Visual Studio, Rider ou:

```powershell
dotnet run --project backend\RidePR.Api\RidePR.Api.csproj
```

Abra o Swagger em:

```text
https://localhost:5001/swagger
```

Se a porta local for diferente, use a URL exibida pelo `dotnet run`.

## 2. Autenticar no Swagger

Crie ou use usuarios existentes em `POST /api/auth/register` e depois faca login em `POST /api/auth/login`.

Exemplo de passageiro:

```json
{
  "name": "Passageiro MVP",
  "email": "passageiro.mvp@ridepr.test",
  "password": "Senha123!",
  "role": 1
}
```

Exemplo de motorista:

```json
{
  "name": "Motorista MVP",
  "email": "motorista.mvp@ridepr.test",
  "password": "Senha123!",
  "role": 2
}
```

Copie o `accessToken`, clique em **Authorize** no Swagger e informe:

```text
Bearer SEU_ACCESS_TOKEN
```

## 3. Preparar motorista online

Garanta que exista um motorista aprovado e online. Use os endpoints de Drivers para cadastrar/aprovar quando necessario.

Depois atualize a localizacao do motorista em `POST /api/driver-location`:

```text
driverId: GUID_DO_DRIVER
latitude: -23.55052
longitude: -46.63331
speed: 0
heading: 0
```

## 4. Passageiro solicita corrida

Use token de passageiro ou administrador em `POST /api/trips`:

```json
{
  "passengerId": "GUID_DO_PASSENGER",
  "origin": "Praca da Se, Sao Paulo",
  "destination": "Avenida Paulista, Sao Paulo",
  "originLatitude": -23.55052,
  "originLongitude": -46.63331,
  "destinationLatitude": -23.56141,
  "destinationLongitude": -46.65588
}
```

Resultado esperado:

- `status`: `Requested`
- `estimatedDistanceKm` preenchido quando o provider de mapas responder
- `estimatedDurationMinutes` preenchido quando o provider de mapas responder
- `price` estimado calculado

Guarde o `id` da corrida.

## 5. Sistema busca motorista e envia oferta

Use `POST /api/dispatch/request`:

```json
{
  "tripId": "GUID_DA_TRIP",
  "radiusKm": 5,
  "timeoutSeconds": 30,
  "maxCandidates": 10
}
```

Resultado esperado:

- `currentOffer` com `driverId`
- lista `candidates`
- motorista escolhido recebe oferta via SignalR quando conectado

## 6. Motorista aceita

Use token de motorista ou administrador em `POST /api/dispatch/accept`:

```json
{
  "tripId": "GUID_DA_TRIP",
  "driverId": "GUID_DO_DRIVER"
}
```

Resultado esperado:

- `status`: `Accepted`
- `driverId` preenchido
- motorista fica `Busy`

Para testar recusa, use `POST /api/dispatch/reject` com:

```json
{
  "tripId": "GUID_DA_TRIP",
  "driverId": "GUID_DO_DRIVER",
  "reason": "Indisponivel"
}
```

## 7. Motorista inicia corrida

Use `POST /api/trips/{tripId}/start`:

```json
{
  "driverId": "GUID_DO_DRIVER"
}
```

Resultado esperado:

- `status`: `InProgress`

## 8. Motorista finaliza corrida

Use `POST /api/trips/{tripId}/finish`:

```json
{
  "driverId": "GUID_DO_DRIVER",
  "actualDistanceKm": 4.2,
  "actualDurationMinutes": 18
}
```

Resultado esperado:

- `status`: `Finished`
- `actualDistanceKm` preenchido
- `price` recalculado
- motorista volta para `Online`

## 9. Consultar corrida

Use:

```text
GET /api/trips/{tripId}
GET /api/trips
```

Confirme a sequencia de status:

```text
Requested -> Accepted -> InProgress -> Finished
```

## 10. Validacao tecnica

Antes de fechar o MVP, rode:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
```

Resultado esperado:

```text
Build succeeded
0 Warnings
0 Errors
Tests passed
```
