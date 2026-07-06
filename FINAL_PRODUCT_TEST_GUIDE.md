# RidePR final product test guide

## Build e validacao

Execute na raiz do repositorio:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
flutter analyze app-passenger
flutter analyze app-driver
cd app-passenger; flutter build apk --debug
cd ..\app-driver; flutter build apk --debug
```

## Visual aprovado

- Admin com fundo superior e menu lateral escuros, logo RidePR amarelo, botoes amarelos para acao principal, verde para sucesso e vermelho para acao destrutiva.
- Passageiro com login escuro, card principal branco, logo RidePR amarelo e fluxo direto para pedir corrida.
- Motorista com login escuro, painel de operacao escuro, destaque amarelo para ficar online/aceitar e informacoes de corrida em linguagem humana.
- Nenhum app ou painel deve mostrar endereco da API, GUID, TripId, DriverId ou dados tecnicos como informacao principal.
- Dados tecnicos, quando necessarios, devem ficar apenas em area tecnica escondida.

## Admin

1. Abrir o painel em `/painel` ou pelo servidor estatico local.
2. Entrar com admin valido.
3. Confirmar que nao aparece campo editavel de API.
4. Em `Corridas`, filtrar por status e filial.
5. Confirmar que a tabela mostra passageiro, motorista, origem, destino e status em portugues.
6. Testar `Ver detalhes`, `Cancelar` e `Finalizar`.
7. Em `Passageiros`, testar aprovar, bloquear e excluir quando nao houver historico.
8. Em `Motoristas`, testar aprovar, bloquear e excluir quando nao houver historico.
9. Em `Veiculos`, testar aprovar, ativar/desativar e excluir quando nao houver historico.
10. Em `Admins`, criar, editar, ativar/desativar e vincular filial.
11. Em `Filiais`, criar, editar, ativar/desativar e definir tarifas.
12. Confirmar que acoes criticas pedem confirmacao.

## Passageiro

1. Abrir `app-passenger`.
2. Confirmar que a primeira tela e login/criar conta.
3. Entrar ou criar conta.
4. Completar cadastro se solicitado.
5. Permitir localizacao.
6. Confirmar mapa ativo, card branco inferior e botoes amarelos.
7. Informar origem/destino legiveis.
8. Pedir corrida.
9. Confirmar status em portugues e atualizacao automatica por SignalR.

## Motorista

1. Abrir `app-driver`.
2. Confirmar que a primeira tela e login/criar conta.
3. Entrar ou criar conta.
4. Completar cadastro de motorista e veiculo.
5. Ativar motorista e veiculo.
6. Ficar online.
7. Receber corrida como oferta atual, sem lista antiga de corridas.
8. Aceitar, iniciar e finalizar corrida.
9. Confirmar painel escuro, status legivel e sem IDs tecnicos.

## Evidencias esperadas

- `app-passenger\build\app\outputs\flutter-apk\app-debug.apk`
- `app-driver\build\app\outputs\flutter-apk\app-debug.apk`
- Build .NET sem erros.
- Testes .NET sem falhas.
- Flutter analyze dos dois apps sem problemas bloqueantes.
