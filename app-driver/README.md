# RidePR Driver App

Flutter app for drivers.

## Run

```bash
flutter pub get
flutter run --dart-define=RIDEPR_API_URL=https://localhost:7045
```

Use an API URL reachable from the emulator/device. For Android emulator, use
`https://10.0.2.2:7045` when the backend is running on the host machine.

## Features

- JWT login with persisted session
- Driver profile loading
- Availability status update
- Driver location update
- Dispatch accept/reject actions
