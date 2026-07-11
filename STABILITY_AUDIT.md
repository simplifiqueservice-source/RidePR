# RidePR stability audit

Data: 10/07/2026

Escopo auditado: backend .NET, `app-driver`, `app-passenger` e `frontend-admin`.

## Objetivo

Estabilizar o fluxo operacional do RidePR com o backend como fonte oficial de estado. A prioridade desta auditoria foi identificar causas de:

- motorista fantasma online;
- divergencia entre painel, apps e backend;
- dispatch com candidato invalido;
- localizacao antiga sendo tratada como presenca real;
- conexoes e timers duplicados;
- corridas que podem ficar pendentes sem limpeza.

## Estados encontrados

### Usuario

Fonte: `User.Active`, `User.Role`.

Papeis atuais:

- `Administrator`
- `Driver`
- `Passenger`

Riscos encontrados:

- Alguns endpoints por `userId` ainda aceitam consulta ampla para roles autenticadas.
- Apps usam o `userId` do login para carregar perfil, o que e correto, mas endpoints ainda precisam de endurecimento progressivo por ownership.

### Motorista

Fonte atual: `Driver.Status`, `Driver.ApprovalStatus`, `Driver.Active`.

Estados atuais:

- `Offline = 1`
- `Online = 2`
- `Busy = 3`
- `Paused = 4`

Status de aprovacao:

- `Pending`
- `Approved`
- `Rejected`

Achados:

- O status `Online` existia como estado persistido, mas podia continuar verdadeiro mesmo sem presenca recente.
- O dispatch filtrava por `DriverStatus.Online`, `Active` e `Approved`, mas dependia de localizacao que podia estar antiga.
- Aprovacao de motorista foi separada do formulario comum do admin; motorista novo nasce pendente.

Correcao aplicada:

- Motorista so conta como online quando tambem possui `DriverLocation.Online = true` e `DriverLocation.UpdatedAt` recente.
- Foi criada limpeza automatica de presenca expirada.
- Ao ficar offline pelo endpoint de status, a localizacao tambem passa a offline.

### Veiculo

Fonte atual: `Vehicle.Active`.

Achados:

- Nao existe enum de aprovacao dedicado para veiculo.
- O painel usa `approve/disable`, mas tecnicamente isso alterna `Active`.
- O dispatch ainda precisa validar explicitamente veiculo ativo/compatibilidade de categoria no fluxo completo.

### Conexao SignalR

Hub atual:

- `/driverHub`

Eventos/metodos encontrados:

- `JoinDriverGroup`
- `LeaveDriverGroup`
- `UpdateLocation`
- `DispatchOfferReceived`
- `DispatchOfferExpired`
- `DispatchUpdated`
- `DriverLocationUpdated`
- eventos genericos de realtime para trip/dispatch no painel

Achados:

- `app-driver`, `app-passenger` e painel usam o mesmo hub.
- Os clientes ja evitam reconectar se a URL atual esta conectada.
- Antes da correcao, `UpdateLocation` aceitava `driverId` informado sem checar ownership.

Correcao aplicada:

- `UpdateLocation` via REST e SignalR agora valida:
  - coordenadas validas;
  - motorista existente;
  - motorista ativo;
  - motorista aprovado;
  - motorista com status online;
  - ownership: motorista so pode atualizar seu proprio `driverId`, exceto admin.

### Disponibilidade/presenca

Fonte oficial apos correcao:

- `Driver.Status = Online`
- `Driver.Active = true`
- `Driver.ApprovalStatus = Approved`
- `DriverLocation.Online = true`
- `DriverLocation.UpdatedAt >= agora - 45 segundos`

Achado principal:

- Antes, motorista podia aparecer online por status/localizacao antiga.

Correcao aplicada:

- `DriverPresenceWorker` roda a cada 15 segundos.
- Se a presenca passar de 45 segundos sem heartbeat/localizacao:
  - `DriverLocation.Online = false`
  - `Driver.Status = Offline`, se estava `Online`

### Oferta/dispatch

Fonte atual:

- fila `IDispatchQueue`, implementada por Redis;
- `DispatchStateDto.CurrentOffer`;
- `DispatchOfferDto`.

Fluxo atual:

1. Passageiro cria corrida.
2. Backend busca motoristas proximos.
3. Backend cria estado de dispatch em Redis.
4. Backend envia oferta ao grupo SignalR do motorista.
5. Motorista aceita ou recusa.
6. Timeout worker expira ofertas.

Achados:

- Oferta ainda nao e uma entidade persistida em banco.
- Estados de oferta existem em DTO/Redis, nao em tabela dedicada.
- Aceite e recusa ja possuem algumas validacoes de idempotencia, mas ainda falta modelo formal de transicoes.
- Ao expirar todos os candidatos, o estado e concluido, mas a corrida ainda merece status final mais claro, como `NoDriverAvailable`.

### Corrida

Fonte atual: `Trip.Status`.

Estados atuais:

- `Requested`
- `Accepted`
- `InProgress`
- `Finished`
- `Cancelled`

Lacunas frente ao objetivo:

- Ainda nao existem estados formais `SearchingDriver`, `OfferSent`, `DriverArriving`, `DriverArrived`, `Started`, `NoDriverAvailable`.
- O app e painel ainda precisam adaptar telas para estados mais ricos.
- O fluxo atual cobre basico, mas nao o ciclo operacional completo descrito no anexo.

### Localizacao

Fonte atual:

- `DriverLocation.Position`
- `DriverLocation.Speed`
- `DriverLocation.Heading`
- `DriverLocation.Online`
- `DriverLocation.UpdatedAt`

Achados:

- `app-driver` usa `Geolocator` e timer de 5 segundos.
- O timer nao inicia duplicado se `locationStreaming` ja esta ativo.
- O app cancela timer no dispose/logout.
- Antes, logout nao mandava offline explicitamente.

Correcao aplicada:

- Logout do `app-driver` tenta mandar status offline antes de parar SignalR.
- Backend nao depende apenas disso: se o app fechar sem logout, o worker remove a presenca.

## Endpoints operacionais mapeados

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `POST /api/auth/forgot-password`
- `GET /api/auth/me`

### Motoristas

- `GET /api/drivers`
- `GET /api/drivers/{id}`
- `GET /api/drivers/by-user/{userId}`
- `POST /api/drivers`
- `PUT /api/drivers/{id}`
- `PATCH /api/drivers/{id}/status`
- `PATCH /api/drivers/{id}/approval`
- `POST /api/drivers/{id}/documents`
- `DELETE /api/drivers/{id}`

### Localizacao

- `POST /api/driver-location`
- `GET /api/driver-location/{driverId}`
- `GET /api/driver-location/nearby`

### Passageiros

- `GET /api/passengers`
- `GET /api/passengers/{id}`
- `GET /api/passengers/by-user/{userId}`
- `GET /api/passengers/{id}/history`
- `POST /api/passengers`
- `PUT /api/passengers/{id}`
- `DELETE /api/passengers/{id}`

### Corridas

- `GET /api/trips`
- `GET /api/trips/{tripId}`
- `POST /api/trips`
- `POST /api/trips/{tripId}/start`
- `POST /api/trips/{tripId}/finish`
- `POST /api/trips/{tripId}/cancel`

### Dispatch

- `GET /api/dispatch/nearby`
- `POST /api/dispatch/request`
- `POST /api/dispatch/start`
- `GET /api/dispatch/{tripId}`
- `POST /api/dispatch/accept`
- `POST /api/dispatch/{tripId}/accept`
- `POST /api/dispatch/reject`
- `POST /api/dispatch/{tripId}/reject`
- `POST /api/dispatch/{tripId}/reassign`

### Mapas e rotas

- `GET /api/maps/providers`
- `POST /api/maps/route`
- `POST /api/maps/distance-matrix`
- `POST /api/maps/eta`
- `POST /api/maps/geocode`
- `POST /api/maps/reverse-geocode`

### Admin

- `GET /api/admin/dashboard`
- `GET /api/admin/trips`
- `GET /api/admin/trips/{id}`
- `POST /api/admin/trips/{id}/cancel`
- `POST /api/admin/trips/{id}/finish`
- `POST /api/admin/trips/{id}/redispatch`
- `POST /api/admin/trips/cancel-old-pending`
- `GET /api/admin/passengers`
- `POST /api/admin/passengers`
- `PUT /api/admin/passengers/{id}`
- `POST /api/admin/passengers/{id}/approve`
- `POST /api/admin/passengers/{id}/block`
- `DELETE /api/admin/passengers/{id}`
- `GET /api/admin/drivers`
- `POST /api/admin/drivers`
- `PUT /api/admin/drivers/{id}`
- `POST /api/admin/drivers/{id}/approve`
- `POST /api/admin/drivers/{id}/block`
- `DELETE /api/admin/drivers/{id}`
- `GET /api/admin/vehicles`
- `POST /api/admin/vehicles`
- `PUT /api/admin/vehicles/{id}`
- `POST /api/admin/vehicles/{id}/approve`
- `POST /api/admin/vehicles/{id}/disable`
- `DELETE /api/admin/vehicles/{id}`
- `GET /api/admin/live-drivers`

## Timers e conexoes

### Backend

- `DispatchTimeoutWorker`: a cada 5 segundos.
- `DriverPresenceWorker`: a cada 15 segundos.

### App driver

- Timer de localizacao: a cada 5 segundos.
- SignalR: uma conexao por URL atual.
- Oferta: contador local de dialogo.

### App passenger

- Timer debounce de busca de destino: 450 ms.
- SignalR: uma conexao por URL atual.

### Painel admin

- SignalR: uma conexao com reconexao automatica.
- Chamadas de refresh ao mudar abas e acoes.

## Dados mockados/coordenadas fixas encontrados

- Apps usam centro inicial fixo `LatLng(-23.555, -46.645)` apenas para renderizar mapa antes de GPS.
- Apps usam URL padrao publica `http://45.185.199.173:8282`.
- Passageiro/motorista temporario criados por registro recebem documento temporario ate completar perfil.
- Nao foi encontrado uso de lista mockada para dispatch real; o fluxo chama backend.

## Correcoes aplicadas nesta etapa

- Presenca valida com TTL de 45 segundos.
- Worker de limpeza de motorista online expirado.
- Dispatch e busca de proximos deixam de usar localizacao antiga.
- Painel admin deixa de contar motorista online por status antigo.
- REST e SignalR bloqueiam update de localizacao de outro motorista.
- Logout do app-driver tenta colocar motorista offline.

## Lacunas restantes

- Criar enums/entidades formais para `DriverAvailability`, `OfferStatus` e estados ricos de `TripStatus`.
- Persistir ofertas em banco com historico/auditoria.
- Validar veiculo ativo/compatibilidade de categoria no dispatch.
- Implementar categorias por filial no fluxo completo do passageiro.
- Desenhar polylines em todas as etapas dos apps e painel.
- Melhorar tela de oferta do motorista com minimapa, som e vibracao.
- Criar testes automatizados cobrindo queda de SignalR, reconexao, expiracao e dupla aceitacao.
- Gerar APKs apos estabilizacao completa do fluxo.

## Conclusao

O maior risco imediato, motorista fantasma online, foi tratado no backend com presenca por heartbeat/localizacao recente e limpeza automatica. O backend ficou mais forte como fonte oficial para online/dispatch. Ainda falta uma segunda etapa para evoluir o modelo de estados de corrida/oferta/categoria e completar a experiencia visual de rotas nos apps.
