# RidePR final product test guide

## Validacao obrigatoria

Execute na raiz do repositorio:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
flutter analyze app-passenger
flutter analyze app-driver
cd app-passenger; flutter build apk --debug
cd ..\app-driver; flutter build apk --debug
```

## Subir API e painel

```powershell
docker compose up -d
dotnet run --project backend\RidePR.Api\RidePR.Api.csproj --launch-profile http
```

A API deve responder em:

```text
http://localhost:8282
http://45.185.199.173:8282
```

Para abrir o painel:

```powershell
cd frontend-admin
python -m http.server 5173
```

Abrir `http://localhost:5173` e manter a BaseUrl padrao `http://45.185.199.173:8282`.

## Fluxo passageiro

1. Abrir `app-passenger`.
2. Escolher `Entrar` ou `Criar conta`.
3. Completar perfil quando solicitado.
4. Permitir localizacao.
5. Ver o mapa com origem real.
6. Informar destino pela busca.
7. Tocar em `Pedir corrida`.
8. Conferir status `Procurando motorista`.
9. Apos aceite, acompanhar status e marcador do motorista.
10. Conferir `Corrida em andamento`.
11. Conferir `Corrida finalizada`.

## Fluxo motorista

1. Abrir `app-driver`.
2. Escolher `Entrar` ou `Criar conta`.
3. Completar cadastro do motorista.
4. Completar cadastro do veiculo.
5. Ativar motorista e veiculo.
6. Tocar em `Ficar online`.
7. Permitir GPS real.
8. Solicitar corrida pelo passageiro.
9. Conferir popup `Nova corrida` com origem, destino e valor.
10. Aceitar ou recusar.
11. Se aceitar, tocar em `Iniciar corrida`.
12. Confirmar envio de localizacao a cada 5 segundos somente online/em corrida.
13. Tocar em `Finalizar corrida`.

## Fluxo admin principal

1. Entrar no painel.
2. Abrir `Filiais` e criar/ativar filial.
3. Abrir `Tarifas` e cadastrar tarifa da filial.
4. Abrir `Admins` e criar admin de filial vinculado.
5. Conferir abas `Dashboard`, `Corridas`, `Motoristas`, `Passageiros`, `Veiculos`, `Mapa`, `Admins`, `Filiais`, `Tarifas` e `Config`.
6. Criar corrida no passageiro e aceitar no motorista.
7. Conferir atualizacao automatica por SignalR.
8. Conferir origem, destino e motorista no mapa sem reset a cada localizacao.

## Fluxo admin filial

1. Entrar com admin filial.
2. Conferir que filiais, admins e tarifas ficam limitados a filial vinculada.
3. Conferir corridas, motoristas e passageiros da filial.
4. Validar status em portugues e dados legiveis.

## Checklist final

- Apps nunca pulam login automaticamente.
- Primeira tela mostra apenas entrada clara: `Entrar` ou `Criar conta`.
- Debug fica fora do fluxo principal.
- Passageiro cai no fluxo de pedir corrida apos perfil completo.
- Motorista so opera depois de motorista e veiculo completos e ativos.
- Dispatch cria oferta e mostra popup no motorista.
- SignalR conecta uma vez por sessao.
- GPS fixo nao e usado como localizacao principal.
- Localizacao duplicada nao e enviada quando o motorista nao se move.
- Painel admin persiste login no navegador.
- Filiais, admins de filial e tarifas por filial funcionam.
- APKs debug sao gerados em:
  - `app-passenger\build\app\outputs\flutter-apk\app-debug.apk`
  - `app-driver\build\app\outputs\flutter-apk\app-debug.apk`
