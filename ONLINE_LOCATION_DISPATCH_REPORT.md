# Online location dispatch report

Data: 2026-07-11 10:45:03

## Resumo

- DriverId: 268041d7-7f36-46a7-bfb8-3da75e7abea9
- PassengerId: 662d8604-b421-4f2f-ba6e-f3dcb766a6ae
- TripId: dc29ca1a-89ac-49d4-8e47-da15816e4669
- OfferDriverId: 268041d7-7f36-46a7-bfb8-3da75e7abea9
- FinalTripStatus: 1

## Etapas

- [OK] HEALTH | /health | 200 | Healthy
- [OK] ADMIN_LOGIN | /api/auth/login | 200 | 6d0f7e08-ce7a-4ab6-a541-4847025a18d9
- [OK] USERS_CREATED | /api/auth/register | 200 | online.pass.104500920@ridepr.test / online.driver.104500920@ridepr.test
- [OK] DRIVER_ID_RESOLVED | /api/admin/drivers | 200 | 268041d7-7f36-46a7-bfb8-3da75e7abea9
- [OK] DRIVER_APPROVED | /api/admin/drivers/268041d7-7f36-46a7-bfb8-3da75e7abea9/approve | 200 | Approved
- [OK] ACTIVE_VEHICLE_CREATED | /api/admin/vehicles | 200 | a295dd4c-e391-40da-b215-3cebb0f09c23
- [OK] DRIVER_LOGIN | /api/auth/login | 200 | 24d698a8-c21a-4cfc-9d2e-862859e46823
- [OK] DRIVER_ONLINE_STATUS | /api/drivers/268041d7-7f36-46a7-bfb8-3da75e7abea9/status | 200 | Online
- [OK] ME_LOCATION_SENT | /api/drivers/me/location | 200 | -25.4284,-49.2733
- [OK] ME_HEARTBEAT_SENT | /api/drivers/me/heartbeat | 200 | 2026-07-11T13:45:02.4308734Z
- [OK] LIVE_DRIVERS_VISIBLE | /api/admin/live-drivers | 200 | -25.4284,-49.2733
- [OK] NEARBY_VISIBLE | /api/driver-location/nearby | 200 | 268041d7-7f36-46a7-bfb8-3da75e7abea9
- [OK] TEST_OFFER_SENT | /api/debug/drivers/268041d7-7f36-46a7-bfb8-3da75e7abea9/test-offer | 200 | DispatchOfferReceived
- [OK] TRIP_CREATED | /api/trips | 200 | dc29ca1a-89ac-49d4-8e47-da15816e4669
- [OK] OFFER_CREATED | /api/dispatch/request | 200 | 268041d7-7f36-46a7-bfb8-3da75e7abea9
- [OK] OFFER_ACCEPTED | /api/dispatch/dc29ca1a-89ac-49d4-8e47-da15816e4669/accept | 200 | 1
