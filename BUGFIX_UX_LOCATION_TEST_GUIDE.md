# RidePR - Guia de Teste UX, Login e Localizacao

## API

1. Suba a API:
   `dotnet run --project backend\RidePR.Api`
2. Confirme Swagger:
   `http://45.185.199.173:8282/swagger`
3. Confirme painel:
   `http://45.185.199.173:8282/painel`

## Passageiro

1. Instale o APK:
   `app-passenger\build\app\outputs\flutter-apk\app-debug.apk`
2. Abra o app.
3. No menu, use **Criar conta** ou **Entrar**.
4. Complete o perfil do passageiro.
5. Na tela principal, informe **Origem** e **Destino**.
6. Toque em **Pedir corrida**.
7. Confirme que:
   - O mapa fica em tela cheia.
   - O card inferior mostra status em portugues.
   - O marcador do motorista aparece quando o app-driver envia localizacao.
   - Debug fica oculto no menu.

## Motorista

1. Instale o APK:
   `app-driver\build\app\outputs\flutter-apk\app-debug.apk`
2. Abra o app.
3. Use **Criar conta** ou **Entrar**.
4. No menu, complete:
   - Cadastro do motorista.
   - Veiculo.
5. Volte para a tela principal.
6. Toque em **Ficar online**.
7. Aceite a permissao de localizacao.
8. Confirme que:
   - Se o GPS estiver desligado, o app mostra erro claro.
   - A localizacao real e enviada a cada 5s somente online ou em corrida.
   - A corrida recebida aparece no card inferior.
   - Os botoes **Aceitar**, **Recusar**, **Iniciar corrida** e **Finalizar corrida** aparecem no momento certo.

## Painel Admin

1. Acesse `/painel`.
2. Faca login como admin.
3. Use as abas:
   - Corridas
   - Motoristas
   - Passageiros
   - Veiculos
   - Mapa
4. Confirme que:
   - Listagens carregam sem erro 500.
   - Status aparecem em portugues.
   - O mapa mostra origem, destino e motorista.
   - SignalR atualiza status e localizacao automaticamente.

## Fluxo Ponta a Ponta

1. Passageiro cria/entra na conta.
2. Passageiro completa perfil.
3. Passageiro pede corrida.
4. Admin ve corrida criada.
5. Motorista cria/entra na conta.
6. Motorista completa motorista e veiculo.
7. Motorista fica online.
8. Motorista recebe corrida.
9. Motorista aceita.
10. Passageiro e admin veem status mudar.
11. Motorista inicia corrida.
12. Motorista envia localizacao real.
13. Passageiro e admin veem motorista no mapa.
14. Motorista finaliza corrida.
15. Passageiro e admin veem status finalizado.
