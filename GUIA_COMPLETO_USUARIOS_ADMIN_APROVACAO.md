# Guia completo - usuarios, admin e aprovacao

Data da validacao: 10/07/2026

Este guia resume o que foi ajustado no RidePR para corrigir cadastro de usuarios, exclusao no painel admin e regra de aprovacao de motoristas.

## Objetivo

Garantir que:

- Passageiros sejam liberados automaticamente apos criar conta/perfil.
- Motoristas fiquem pendentes apos finalizar o cadastro.
- Motoristas so sejam aprovados por acao do admin.
- Usuarios criados pela plataforma aparecam corretamente no painel admin.
- Passageiros e motoristas possam ser excluidos quando nao possuem vinculos.
- Passageiros e motoristas com historico sejam desativados, preservando dados importantes.

## Regras atuais

### Passageiro

- Ao criar conta como passageiro, o backend cria automaticamente o perfil de passageiro.
- O passageiro nasce ativo/aprovado.
- No painel admin, criar passageiro tambem cria o usuario e o perfil como ativo.
- O campo `Active` pode ser usado depois para bloquear/desbloquear, mas a criacao inicial e livre.

### Motorista

- Ao criar conta como motorista, o backend cria automaticamente o perfil de motorista.
- O motorista nasce:
  - `ApprovalStatus = Pending`
  - `Status = Offline`
  - `Active = true`
- Criar motorista pelo painel admin tambem deixa o motorista pendente.
- Salvar/editar dados do motorista no painel nao aprova o motorista.
- A aprovacao acontece somente pela acao admin:
  - `POST /api/admin/drivers/{id}/approve`
- Motorista nao aprovado nao deve operar como online.

## Arquivos alterados

### `backend/RidePR.Api/Controllers/AuthController.cs`

Foi adicionada criacao automatica de perfil apos registro:

- Registro de passageiro cria um `Passenger`.
- Registro de motorista cria um `Driver`.
- Passageiro fica ativo automaticamente.
- Motorista fica pendente e offline.
- Tambem existe endpoint de redefinicao de senha:
  - `POST /api/auth/forgot-password`

### `backend/RidePR.Api/Controllers/AdminOperationsController.cs`

Foram corrigidas regras do painel admin:

- Passageiro criado no admin nasce ativo, mesmo se o payload mandar `active=false`.
- Motorista criado no admin nasce pendente/offline, mesmo se o payload mandar `approved=true`.
- Edicao de motorista nao altera `ApprovalStatus`.
- Exclusao de passageiro/motorista agora funciona em dois modos:
  - Sem historico: remove usuario, perfil e refresh tokens.
  - Com historico/vinculo: desativa o cadastro, preservando os dados.
- Desativacao de motorista tambem coloca status offline e desativa veiculos relacionados.

### `frontend-admin/index.html`

Foi ajustado o campo de aprovacao do motorista:

- Antes o painel sugeria que o formulario poderia salvar motorista como aprovado.
- Agora o campo aparece como `Aprovacao` e fica desabilitado no formulario.
- A aprovacao deve ser feita pela acao especifica do admin.

### `scripts/admin-user-delete-smoke.ps1`

Foi criado/atualizado um smoke test que valida o fluxo real na API:

- Login admin.
- Criacao de passageiro e motorista via `/api/auth/register`.
- Verificacao de perfil criado no painel admin.
- Passageiro nasce ativo.
- Motorista nasce pendente.
- Motorista e aprovado pelo endpoint admin.
- Criacao de passageiro e motorista diretamente pelo painel admin.
- Exclusao fisica quando nao ha historico.
- Desativacao quando ha viagem, veiculo ou outro vinculo.

## Como testar

Com a API rodando em `http://127.0.0.1:8282`, execute:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\admin-user-delete-smoke.ps1
```

Resultado esperado:

- `passengerAutoApproved: true`
- `driverInitialApproval: Pending`
- `driverAdminApproval: Approved`
- `adminPassengerAutoApproved: true`
- `adminDriverInitialApproval: Pending`
- `deletePassenger: Deleted`
- `deleteDriver: Deleted`
- `softDeletePassenger: Deactivated`
- `softDeleteDriver: Deactivated`

## Validacoes executadas

Foram executados estes comandos:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
node --check frontend-admin\app.js
powershell -ExecutionPolicy Bypass -File scripts\admin-user-delete-smoke.ps1
```

Resultado:

- Build .NET: sucesso, 0 avisos e 0 erros.
- Testes .NET: sucesso, 1 teste aprovado.
- Checagem JS do painel: sucesso.
- Smoke test de usuarios/admin: sucesso.

## URLs para teste local

- API: `http://127.0.0.1:8282`
- Health: `http://127.0.0.1:8282/health`
- Painel admin: `http://127.0.0.1:8282/painel`
- App passageiro web: `http://127.0.0.1:3101`
- App motorista web: `http://127.0.0.1:3102`

## Fluxo recomendado para testar manualmente

1. Abra o painel admin.
2. Crie um passageiro.
3. Confirme que ele aparece como ativo/aprovado.
4. Crie um motorista.
5. Confirme que ele aparece como pendente.
6. Use a acao de aprovar motorista.
7. Confirme que o status muda para aprovado.
8. Tente excluir um usuario sem historico e confirme que a acao retorna `Deleted`.
9. Tente excluir um usuario com viagem/veiculo e confirme que a acao retorna `Deactivated`.

## Observacoes

- O painel admin deve ser a fonte de aprovacao de motoristas.
- Passageiro nao passa por fila de aprovacao.
- Motorista pode ter conta e perfil criados, mas precisa de aprovacao admin para operar.
- Quando ha historico operacional, o sistema preserva dados e usa desativacao em vez de apagar definitivamente.
