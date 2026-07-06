# RidePR UX, performance and location fix guide

## Base URL

All apps and the admin panel keep the default API URL:

```text
http://45.185.199.173:8282
```

## Passenger flow

1. Open the passenger app.
2. Use **Entrar**, **Criar conta** or **Recuperar senha** on the first screen.
3. After login, complete the passenger profile if the app asks for it.
4. Confirm origin and destination on the map screen.
5. Tap **Pedir corrida**.
6. The app creates the trip and requests dispatch automatically.
7. Watch the trip status and driver marker update through SignalR.
8. Debug and advanced fields stay inside the menu/configuration areas.

## Driver flow

1. Open the driver app.
2. Use **Entrar**, **Criar conta** or **Recuperar senha** on the first screen.
3. Complete the driver registration.
4. Complete vehicle registration.
5. Keep both driver and vehicle active.
6. Tap **Ficar online**.
7. The app starts real GPS location streaming every 5 seconds.
8. New offers appear as a clear card with origin, destination and **Aceitar** / **Recusar**.
9. After accepting, use **Iniciar corrida** and then **Finalizar corrida**.
10. Location is sent only while online or in an active trip, and duplicate stationary positions are ignored.

## Location checks

- GPS disabled: the driver app shows a clear message asking the user to enable phone location.
- Permission denied: the driver app asks for permission and shows a clear denied message if the user refuses.
- Permission blocked forever: the app tells the user to enable permission in Android settings.
- Fixed coordinates are not used as the primary driver location.
- The passenger app and admin panel receive `DriverLocationUpdated` through the existing SignalR hub.

## Admin flow

1. Open `frontend-admin/index.html`.
2. Login with an administrator account.
3. Use the tabs: **Corridas**, **Motoristas**, **Passageiros**, **Veiculos**, **Mapa**.
4. Corridas show readable Portuguese statuses.
5. Tables show users, drivers, passengers and vehicles in legible columns.
6. The map shows origin, destination and driver marker when available.
7. SignalR updates local UI state automatically without duplicating handlers or resetting the map on every driver location update.

## Required validation

Run these commands from the repository root:

```powershell
dotnet build backend\RidePR.sln
dotnet test backend\RidePR.sln
flutter analyze app-passenger
flutter analyze app-driver
Set-Location app-passenger; flutter build apk --debug; Set-Location ..
Set-Location app-driver; flutter build apk --debug; Set-Location ..
```
