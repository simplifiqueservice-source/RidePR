# RidePR Realtime + Map Test Guide

## API

1. Suba o backend:

```powershell
dotnet run --project backend\RidePR.Api --urls "http://0.0.0.0:5090"
```

2. Use a URL local no PC:

```text
http://127.0.0.1:5090
```

3. Use a URL da rede no telefone:

```text
http://192.168.1.15:5090
```

## Painel admin

1. Abra:

```text
http://192.168.1.15:5090/painel
```

2. Faca login com:

```text
admin@ridepr.test
Senha123!
```

3. Confirme que aparece `SignalR conectado`.

4. Siga o fluxo:

- Criar corrida
- Solicitar dispatch
- Aceitar
- Iniciar
- Finalizar

5. O mapa deve mostrar:

- origem
- destino
- motorista quando houver `DriverLocationUpdated`

6. O painel deve receber automaticamente:

- `TripRequested`
- `TripAccepted`
- `TripStarted`
- `TripFinished`
- `DriverLocationUpdated`

## App passageiro

1. Abra o app em `app-passenger`.
2. Informe a API baseUrl:

```text
http://192.168.1.15:5090
```

3. Faca login com:

```text
passageiro.mvp@ridepr.test
Senha123!
```

4. Confirme que aparece `SignalR conectado`.
5. Crie uma corrida ou cole um `TripId` existente.
6. Na aba Status, acompanhe o mapa e o status em tempo real.

## Atualizar localizacao do motorista

Pelo Swagger ou painel/teste, envie:

```http
POST /api/driver-location?driverId=d4ff8255-d3fe-4fb6-9fb9-2daaae8398c1&latitude=-23.55052&longitude=-46.63331&speed=0&heading=0
```

Depois envie outra coordenada proxima:

```http
POST /api/driver-location?driverId=d4ff8255-d3fe-4fb6-9fb9-2daaae8398c1&latitude=-23.56141&longitude=-46.65588&speed=20&heading=90
```

O marcador do motorista deve mover no painel e no app passageiro.
