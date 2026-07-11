# RidePR admin bug audit

Data: 10/07/2026

Escopo: `frontend-admin`, endpoints `/api/admin/*`, `/api/admin-panel/*` e eventos SignalR consumidos pelo painel.

## Resumo

O painel admin ja possui uma base utilizavel para operacao, mas alguns indicadores dependiam de estado antigo do backend. A principal correcao aplicada nesta etapa foi impedir que motorista sem presenca recente continue aparecendo como online no dashboard, mapa e listas operacionais.

## Dashboard

### Achado

O contador de motoristas online usava `Driver.Status == Online` em partes do backend.

### Risco

Um motorista podia ficar online no painel mesmo depois de fechar o app, perder conexao ou parar de enviar localizacao.

### Correcao aplicada

O online agora considera:

- `DriverLocation.Online = true`
- `DriverLocation.UpdatedAt` dentro de 45 segundos
- motorista ativo
- motorista aprovado
- status do motorista online

## Mapa

### Achado

O painel consome `/api/admin/live-drivers` e evento `DriverLocationUpdated`.

### Risco

Localizacao antiga podia continuar no mapa se `Online` permanecesse verdadeiro.

### Correcao aplicada

`/api/admin/live-drivers` agora retorna `Online=false` para presenca expirada e filtra corretamente quando `onlineOnly=true`.

## SignalR

### Achado

O painel conecta no `/driverHub` e recebe `DriverLocationUpdated`.

### Risco

Antes, o hub aceitava update de localizacao com `driverId` informado pelo cliente sem validar ownership.

### Correcao aplicada

O hub agora valida que:

- coordenadas sao validas;
- motorista existe;
- motorista esta ativo;
- motorista esta aprovado;
- motorista esta online;
- usuario Driver so atualiza o proprio motorista;
- admin pode atualizar qualquer motorista.

## Acoes de motoristas

### Achado

O formulario de motorista tinha campo visual de aprovacao.

### Risco

Salvar dados cadastrais poderia ser confundido com aprovacao operacional.

### Correcao aplicada

O campo de aprovacao ficou desabilitado no formulario. A aprovacao deve ocorrer pela acao especifica de admin.

## Exclusao/desativacao

### Achado

Usuarios criados pela plataforma podiam falhar na exclusao quando havia vinculos.

### Correcao aplicada

O backend agora separa:

- sem historico: exclusao de usuario, perfil e refresh tokens;
- com historico: desativacao para preservar dados.

## Filtros e refresh

### Achado

O painel recarrega listas apos acoes principais.

### Risco restante

Alguns refreshes ainda sao manuais e podem nao refletir eventos incrementais de todas as entidades.

### Proxima acao recomendada

Padronizar eventos SignalR para:

- motorista ficou online;
- motorista ficou offline por timeout;
- corrida mudou de status;
- oferta enviada/expirada/aceita;
- painel deve atualizar somente dados afetados.

## Dados tecnicos visiveis

### Achado

O painel ainda mostra alguns identificadores e mensagens tecnicas em areas de operacao/teste.

### Proxima acao recomendada

Separar modo operacional de modo diagnostico. IDs e payloads devem ficar em area de suporte/debug, nao no fluxo normal.

## Conclusao

O painel ficou mais consistente para motoristas online e regras de aprovacao/exclusao. O proximo bloco deve focar em eventos incrementais mais claros e em mapa/corridas ativas com estado oficial do backend, sem depender de refresh amplo.
