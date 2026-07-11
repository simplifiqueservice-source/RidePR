# Driver dispatch diagnostic report

Data: 2026-07-11 10:11:13

## Resumo

- DriverId: c54cd308-2945-455e-8af8-fb1fad9e7403
- PassengerId: f256fd86-998a-4979-aad5-0c1e690f0a62
- VehicleId: cba9f4db-3aea-450d-b483-aca2d4d16aca
- TripId: 55b494ac-8b30-4827-9e44-1117b4a156fe
- OfferDriverId: c54cd308-2945-455e-8af8-fb1fad9e7403
- FinalTripStatus: 1

## Etapas

- [OK] HEALTH | /health | 200 | Healthy
- [OK] ADMIN_LOGIN | /api/auth/login | 200 | 6d0f7e08-ce7a-4ab6-a541-4847025a18d9
- [OK] PASSENGER_REGISTER | /api/auth/register | 200 | diag.pass.101110292@ridepr.test
- [OK] DRIVER_REGISTER | /api/auth/register | 200 | diag.driver.101110292@ridepr.test
- [OK] DRIVER_PROFILE_LOADED | /api/admin/drivers | 200 | c54cd308-2945-455e-8af8-fb1fad9e7403
- [OK] DRIVER_APPROVED | /api/admin/drivers/c54cd308-2945-455e-8af8-fb1fad9e7403/approve | 200 | Approved
- [OK] VEHICLE_CREATED | /api/admin/vehicles | 200 | cba9f4db-3aea-450d-b483-aca2d4d16aca
- [OK] DRIVER_LOGIN | /api/auth/login | 200 | 9a3b154e-44dc-4b19-b729-d82f7661a842
- [OK] DRIVER_ONLINE_CONFIRMED | /api/drivers/c54cd308-2945-455e-8af8-fb1fad9e7403/status | 200 | Online
- [OK] HEARTBEAT_LOCATION_SENT | /api/driver-location | 200 | lat=-25.4284 lng=-49.2733
- [OK] LIVE_DRIVERS_CONFIRMED | /api/admin/live-drivers | 200 | c54cd308-2945-455e-8af8-fb1fad9e7403
- [OK] DEBUG_TEST_OFFER_SENT | /api/debug/drivers/c54cd308-2945-455e-8af8-fb1fad9e7403/test-offer | 200 | 3b544843-6be2-4319-b7cc-443df2441515
- [OK] TRIP_CREATED | /api/trips | 200 | 55b494ac-8b30-4827-9e44-1117b4a156fe
- [OK] DISPATCH_STARTED | /api/dispatch/request | 200 | candidates=1
- [OK] OFFER_CREATED | /api/dispatch/request | 200 | driverId=c54cd308-2945-455e-8af8-fb1fad9e7403
- [OK] OFFER_ACCEPTED | /api/dispatch/55b494ac-8b30-4827-9e44-1117b4a156fe/accept | 200 | 1

## Observacao

Este diagnostico comprova login, DriverId, aprovacao, veiculo ativo, online no backend, heartbeat/localizacao, live-drivers, disparo de oferta de teste, dispatch real e aceite. A confirmacao visual do popup deve ser vista no app-driver aberto, pelos logs DISPATCH_EVENT_RECEIVED e OFFER_DIALOG_OPENED.
