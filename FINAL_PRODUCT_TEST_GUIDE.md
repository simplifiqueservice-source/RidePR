# RidePR final product test guide

## Subir API e servicos

Na raiz do repositorio:

```powershell
docker compose up -d
dotnet run --project backend\RidePR.Api\RidePR.Api.csproj --launch-profile http
```

A API deve responder em:

```text
http://localhost:5090
http://localhost:8282
http://45.185.199.173:8282
```

## Abrir painel admin

Opção simples:

```powershell
cd frontend-admin
python -m http.server 5173
```

Abrir:

```text
http://localhost:5173
```

O painel usa a BaseUrl padrão:

```text
http://45.185.199.173:8282
```

## Gerar e instalar APKs

Gerar APK motorista:

```powershell
cd app-driver
flutter build apk --debug
```

APK:

```text
app-driver\build\app\outputs\flutter-apk\app-debug.apk
```

Gerar APK passageiro:

```powershell
cd app-passenger
flutter build apk --debug
```

APK:

```text
app-passenger\build\app\outputs\flutter-apk\app-debug.apk
```

Instalar no celular com USB:

```powershell
adb install -r build\app\outputs\flutter-apk\app-debug.apk
```

## Fluxo do motorista

1. Abrir app-driver.
2. Entrar ou criar conta.
3. Completar cadastro do motorista.
4. Completar cadastro do veiculo.
5. Ativar motorista e veiculo.
6. Tocar em **Ficar online**.
7. Permitir localizacao.
8. Confirmar que o status muda para aguardando corridas.
9. Solicitar corrida pelo app passageiro.
10. Confirmar popup **Nova corrida** com origem, destino, aceitar e recusar.
11. Aceitar corrida.
12. Tocar em **Iniciar corrida**.
13. Confirmar GPS enviando localizacao real.
14. Tocar em **Finalizar corrida**.
15. Abrir **Minhas corridas** e confirmar a corrida no historico.

## Fluxo do passageiro

1. Abrir app-passenger.
2. Entrar ou criar conta.
3. Completar perfil do passageiro.
4. Permitir localizacao.
5. Confirmar origem pela localizacao atual.
6. Buscar destino pelo campo de destino.
7. Selecionar sugestao.
8. Tocar em **Pedir corrida**.
9. Confirmar status **Procurando motorista**.
10. Quando motorista aceitar, confirmar status **Motorista a caminho**.
11. Confirmar marcador do motorista no mapa.
12. Confirmar status **Corrida em andamento**.
13. Confirmar **Corrida finalizada**.
14. Abrir **Minhas corridas** e verificar historico.

## Fluxo do admin

1. Abrir painel admin.
2. Fazer login como administrador.
3. Conferir Dashboard:
   - Corridas aguardando.
   - Corridas em andamento.
   - Motoristas online.
   - Passageiros cadastrados.
   - Corridas finalizadas hoje.
4. Abrir Corridas e verificar status em portugues.
5. Abrir Motoristas, Passageiros e Veiculos.
6. Abrir Mapa.
7. Criar corrida pelo passageiro e aceitar pelo motorista.
8. Confirmar atualizacao automatica por SignalR.
9. Confirmar que o mapa nao reseta a cada localizacao.

## Checklist do teste definitivo

- API abre em `http://localhost:8282`.
- Admin abre em `http://localhost:5173`.
- Driver nao mostra JSON, ID tecnico ou lista velha de corridas.
- Passageiro nao mostra JSON, ID tecnico ou debug.
- Motorista recebe popup de corrida.
- GPS real do motorista e passageiro funciona.
- Localizacao aparece no passageiro e admin.
- SignalR conecta uma vez por sessao.
- Historico do motorista mostra somente corridas dele.
- Historico do passageiro mostra somente corridas dele.
- APKs debug gerados nos caminhos esperados.
