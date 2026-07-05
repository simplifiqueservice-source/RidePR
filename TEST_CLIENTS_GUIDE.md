# RidePR Test Clients Guide

Guia rapido para testar o MVP RidePR usando:

- `app-passenger`: app Flutter de teste para telefone.
- `frontend-admin`: painel web estatico para navegador.

## 1. Subir a API

Na raiz do repositorio:

```powershell
dotnet run --project backend\RidePR.Api --urls "http://0.0.0.0:5090"
```

No telefone e no painel web, use:

```text
http://192.168.1.15:5090
```

O telefone precisa estar na mesma rede Wi-Fi do computador. Se nao conectar, confira o firewall do Windows liberando a porta `5090`.

## 2. Testar no telefone com app-passenger

Entre em `app-passenger` e rode:

```powershell
flutter pub get
flutter run
```

No app:

1. Abra a aba `Login`.
2. Informe a `Base URL da API`, por exemplo `http://192.168.1.15:5090`.
3. Informe e-mail e senha do passageiro.
4. Toque em `Entrar`.
5. Abra `Solicitar`.
6. Informe `PassengerId`, origem, destino e coordenadas.
7. Toque em `Criar corrida`.
8. Copie ou confirme o `TripId` preenchido.
9. Abra `Testes`.
10. Informe `DriverId manual`.
11. Toque em `Solicitar dispatch`.
12. Toque em `Aceitar corrida`.
13. Toque em `Iniciar corrida`.
14. Toque em `Finalizar corrida`.
15. Abra `Status` e toque em `Consultar corrida`.

Todas as respostas JSON aparecem no painel inferior do app.

## 3. Testar no navegador com frontend-admin

Abra:

```text
frontend-admin/index.html
```

No painel:

1. Informe `API baseUrl`, por exemplo `http://192.168.1.15:5090`.
2. Informe e-mail e senha de admin.
3. Clique em `Login`.
4. Use os botoes:
   - `Listar usuarios`
   - `Listar motoristas`
   - `Listar veiculos`
   - `Listar passageiros`
   - `Listar corridas`
5. Para testar corrida, informe `PassengerId`.
6. Clique em `Criar corrida`.
7. Informe `DriverId manual`.
8. Clique em `Solicitar dispatch`.
9. Clique em `Aceitar`.
10. Clique em `Iniciar`.
11. Clique em `Finalizar`.
12. Clique em `Buscar status`.

Todas as respostas JSON aparecem na area `Resposta JSON`.

## 4. Dados necessarios

Para o fluxo funcionar, o backend precisa ter:

- Usuario passageiro com login valido.
- Cadastro de passageiro vinculado ao usuario.
- Usuario motorista com login valido.
- Cadastro de motorista aprovado e online.
- Localizacao online do motorista perto da origem.
- Usuario admin para usar o painel web.

## 5. Validacoes

Backend:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
```

Flutter:

```powershell
cd app-passenger
flutter pub get
flutter analyze
```

Frontend admin:

Nao ha dependencias npm neste MVP. E um painel HTML/CSS/JS estatico.
