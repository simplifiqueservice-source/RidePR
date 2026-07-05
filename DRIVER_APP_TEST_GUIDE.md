# RidePR Driver App Test Guide

## Preparar backend

Suba a API:

```powershell
dotnet run --project backend\RidePR.Api --urls "http://0.0.0.0:5090"
```

No telefone, use:

```text
http://192.168.1.15:5090
```

## Login motorista

No `app-driver`, faca login com:

```text
motorista.mvp@ridepr.test
Senha123!
```

Confirme que aparece:

```text
SignalR conectado.
```

## Fluxo completo

1. No painel admin ou app passageiro, crie uma corrida.
2. Clique em `Solicitar dispatch`.
3. No app-driver, confirme que a oferta aparece em `Oferta em tempo real`.
4. Toque em `Aceitar`.
5. Em `Corrida atual`, toque em `Iniciar`.
6. Em `Localizacao em tempo real`, toque em `Enviar agora` ou `Enviar a cada 5s`.
7. Confirme no painel/admin ou app passageiro que o marcador do motorista atualiza.
8. Informe distancia/duracao final e toque em `Finalizar`.

## Corridas disponiveis

A secao `Corridas disponiveis` lista corridas em status `Requested` sem motorista atribuido.

Para corrida criada via dispatch, prefira aceitar pela secao `Oferta em tempo real`.
