# RidePR End-to-End MVP Test Guide

## Objetivo

Validar o mesmo fluxo ao vivo no painel admin, app passageiro e app motorista.

## Backend

Suba a API:

```powershell
dotnet run --project backend\RidePR.Api --urls "http://0.0.0.0:5090"
```

No PC:

```text
http://127.0.0.1:5090
```

No telefone:

```text
http://192.168.1.15:5090
```

## Contas de teste

Admin:

```text
admin@ridepr.test
Senha123!
```

Passageiro:

```text
passageiro.mvp@ridepr.test
Senha123!
```

Motorista:

```text
motorista.mvp@ridepr.test
Senha123!
```

IDs de teste:

```text
PassengerId: 9e0d144e-6446-4f6a-a016-ac7cecd2d7b8
DriverId: d4ff8255-d3fe-4fb6-9fb9-2daaae8398c1
```

## Ordem do teste

1. Abra o painel admin em `/painel`.
2. Faca login no painel e confirme `SignalR conectado`.
3. Abra o app passageiro, faca login e confirme `SignalR conectado`.
4. Abra o app motorista, faca login e confirme `SignalR conectado`.
5. No app motorista, deixe o status como `Online`.
6. No painel ou app passageiro, clique em `Criar corrida`.
7. Confirme que o `TripId` foi preenchido.
8. Clique em `Solicitar dispatch`.
9. No app motorista, a oferta deve aparecer em `Oferta em tempo real`.
10. No app motorista, clique em `Aceitar`.
11. No painel e no app passageiro, o status deve mudar para `Aceita`.
12. No app motorista, clique em `Iniciar`.
13. No painel e no app passageiro, o status deve mudar para `Em andamento`.
14. No app motorista, clique em `Enviar agora` ou `Enviar a cada 5s`.
15. No painel e no app passageiro, o marcador do motorista deve aparecer ou mover no mapa.
16. No app motorista, clique em `Finalizar`.
17. No painel e no app passageiro, o status deve mudar para `Finalizada`.

## Sinais esperados

- Admin: mapa mostra origem, destino e motorista.
- Passageiro: mapa mostra origem, destino e motorista.
- Motorista: recebe oferta via `DispatchOfferReceived`.
- Todos os clientes mostram mensagens de SignalR claras.
- Botoes indisponiveis ficam desabilitados quando falta login, `TripId` ou `DriverId`.

## Problemas comuns

- Se nao aparecer `SignalR conectado`, confira a `baseUrl`.
- Se o motorista nao receber oferta, confirme que ele esta `Online`.
- Se `Iniciar` ou `Finalizar` ficar desabilitado, confirme que a corrida foi aceita pelo motorista.
- Se o mapa nao carregar, confira se o telefone tem internet para baixar tiles do OpenStreetMap.
