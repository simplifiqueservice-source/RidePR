# RidePR Passenger App

Flutter app for passengers.

## Run

```bash
flutter pub get
flutter run --dart-define=RIDEPR_API_URL=https://localhost:7045
```

Use an API URL reachable from the emulator/device. For Android emulator, use
`https://10.0.2.2:7045` when the backend is running on the host machine.

## Features

- JWT login with persisted session
- Passenger profile loading
- Wallet balance loading
- Trip request form integrated with `POST /api/Trips`
