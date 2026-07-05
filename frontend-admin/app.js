let accessToken = '';
let hubConnection = null;
let map = null;
let originMarker = null;
let destinationMarker = null;
let driverMarker = null;
let currentTrip = null;

const $ = (id) => document.getElementById(id);

function baseUrl() {
  return $('baseUrl').value.trim().replace(/\/+$/, '');
}

function headers() {
  return {
    'Content-Type': 'application/json',
    ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
  };
}

function numberValue(id) {
  const value = $(id).value.trim().replace(',', '.');
  return Number.parseFloat(value) || 0;
}

function show(statusCode, body) {
  const payload = typeof body === 'string' ? body : JSON.stringify(body, null, 2);
  $('responseBox').textContent = `HTTP ${statusCode}\n${payload || '(no body)'}`;
}

function setLiveStatus(message) {
  $('liveStatus').textContent = message;
}

function field(source, name) {
  if (!source) {
    return undefined;
  }

  const camel = name.charAt(0).toLowerCase() + name.slice(1);
  const pascal = name.charAt(0).toUpperCase() + name.slice(1);

  return source[camel] ?? source[pascal] ?? source[name];
}

function initMap() {
  if (map || !window.L) {
    return;
  }

  map = L.map('map').setView([-23.555, -46.645], 13);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '&copy; OpenStreetMap contributors',
    maxZoom: 19,
  }).addTo(map);
}

function markerAt(existingMarker, latitude, longitude, label) {
  if (!map || !Number.isFinite(latitude) || !Number.isFinite(longitude)) {
    return existingMarker;
  }

  if (existingMarker) {
    existingMarker.setLatLng([latitude, longitude]).bindPopup(label);
    return existingMarker;
  }

  return L.marker([latitude, longitude]).addTo(map).bindPopup(label);
}

function fitMap() {
  if (!map) {
    return;
  }

  const points = [originMarker, destinationMarker, driverMarker]
    .filter(Boolean)
    .map((marker) => marker.getLatLng());

  if (points.length === 1) {
    map.setView(points[0], 15);
    return;
  }

  if (points.length > 1) {
    map.fitBounds(L.latLngBounds(points), { padding: [36, 36] });
  }
}

function updateTripMap(trip, eventName = 'Corrida atualizada') {
  currentTrip = trip;
  initMap();

  const originLatitude = Number(field(trip, 'OriginLatitude'));
  const originLongitude = Number(field(trip, 'OriginLongitude'));
  const destinationLatitude = Number(field(trip, 'DestinationLatitude'));
  const destinationLongitude = Number(field(trip, 'DestinationLongitude'));
  const status = field(trip, 'Status') ?? 'Status desconhecido';
  const tripId = field(trip, 'Id');

  originMarker = markerAt(originMarker, originLatitude, originLongitude, 'Origem');
  destinationMarker = markerAt(destinationMarker, destinationLatitude, destinationLongitude, 'Destino');
  fitMap();
  setLiveStatus(`${eventName}: ${status}${tripId ? ` (${tripId})` : ''}`);
}

function updateDriverLocation(location) {
  initMap();

  const latitude = Number(field(location, 'Latitude'));
  const longitude = Number(field(location, 'Longitude'));
  const driverId = field(location, 'DriverId') ?? $('driverId').value.trim();

  driverMarker = markerAt(driverMarker, latitude, longitude, `Motorista ${driverId}`);
  fitMap();
  setLiveStatus(`DriverLocationUpdated: ${driverId}`);
}

function requireValue(id, label) {
  const value = $(id).value.trim();

  if (!value) {
    show('LOCAL', `${label} obrigatorio. Crie uma corrida primeiro ou preencha o campo manualmente.`);
    $(id).focus();
    return null;
  }

  return value;
}

async function request(path, options = {}) {
  try {
    const response = await fetch(`${baseUrl()}${path}`, {
      ...options,
      headers: {
        ...headers(),
        ...(options.headers || {}),
      },
    });
    const text = await response.text();
    let body = text;

    if (text) {
      try {
        body = JSON.parse(text);
      } catch {
        body = text;
      }
    }

    show(response.status, body);
    return { response, body };
  } catch (error) {
    show('LOCAL', String(error));
    return { response: null, body: null };
  }
}

async function post(path, body) {
  return request(path, {
    method: 'POST',
    body: JSON.stringify(body),
  });
}

async function login() {
  const { response, body } = await post('/api/auth/login', {
    email: $('email').value.trim(),
    password: $('password').value,
  });

  if (response?.ok && body?.accessToken) {
    accessToken = body.accessToken;
    $('tokenStatus').textContent = 'Token admin salvo em memoria.';
    await connectRealtime();
  }
}

async function list(path) {
  await request(path);
}

async function createTrip() {
  const { response, body } = await post('/api/trips', {
    passengerId: $('passengerId').value.trim(),
    origin: $('origin').value.trim(),
    destination: $('destination').value.trim(),
    originLatitude: numberValue('originLatitude'),
    originLongitude: numberValue('originLongitude'),
    destinationLatitude: numberValue('destinationLatitude'),
    destinationLongitude: numberValue('destinationLongitude'),
  });

  if (response?.ok && body?.id) {
    $('tripId').value = body.id;
    updateTripMap(body, 'TripRequested');
  }
}

async function getTrip() {
  const tripId = requireValue('tripId', 'TripId');

  if (!tripId) {
    return;
  }

  const { response, body } = await request(`/api/trips/${tripId}`);

  if (response?.ok && body) {
    updateTripMap(body, 'Status consultado');
  }
}

async function requestDispatch() {
  const tripId = requireValue('tripId', 'TripId');

  if (!tripId) {
    return;
  }

  await post('/api/dispatch/request', {
    tripId,
    radiusKm: numberValue('radiusKm'),
    timeoutSeconds: 30,
    maxCandidates: 10,
  });
}

async function acceptTrip() {
  const tripId = requireValue('tripId', 'TripId');
  const driverId = requireValue('driverId', 'DriverId');

  if (!tripId || !driverId) {
    return;
  }

  await post('/api/dispatch/accept', {
    tripId,
    driverId,
  });
}

async function startTrip() {
  const tripId = requireValue('tripId', 'TripId');
  const driverId = requireValue('driverId', 'DriverId');

  if (!tripId || !driverId) {
    return;
  }

  await post(`/api/trips/${tripId}/start`, {
    driverId,
  });
}

async function finishTrip() {
  const tripId = requireValue('tripId', 'TripId');
  const driverId = requireValue('driverId', 'DriverId');

  if (!tripId || !driverId) {
    return;
  }

  await post(`/api/trips/${tripId}/finish`, {
    driverId,
    actualDistanceKm: numberValue('actualDistanceKm'),
    actualDurationMinutes: numberValue('actualDurationMinutes'),
  });
}

async function connectRealtime() {
  if (!window.signalR) {
    setLiveStatus('SignalR JS nao carregado.');
    return;
  }

  if (hubConnection) {
    await hubConnection.stop();
  }

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl()}/driverHub`, {
      accessTokenFactory: () => accessToken,
    })
    .withAutomaticReconnect()
    .build();

  const onTrip = (eventName) => (trip) => {
    updateTripMap(trip, eventName);
    show('SIGNALR', { event: eventName, data: trip });
  };

  hubConnection.on('TripRequested', onTrip('TripRequested'));
  hubConnection.on('TripAccepted', onTrip('TripAccepted'));
  hubConnection.on('TripStarted', onTrip('TripStarted'));
  hubConnection.on('TripFinished', onTrip('TripFinished'));
  hubConnection.on('DriverLocationUpdated', (locationOrDriverId, latitude, longitude, speed, heading) => {
    const location = typeof locationOrDriverId === 'object'
      ? locationOrDriverId
      : { driverId: locationOrDriverId, latitude, longitude, speed, heading };

    updateDriverLocation(location);
    show('SIGNALR', { event: 'DriverLocationUpdated', data: location });
  });

  hubConnection.onreconnecting(() => setLiveStatus('SignalR reconectando...'));
  hubConnection.onreconnected(() => setLiveStatus('SignalR conectado novamente.'));
  hubConnection.onclose(() => setLiveStatus('SignalR desconectado.'));

  await hubConnection.start();
  setLiveStatus('SignalR conectado.');
}

$('loginButton').addEventListener('click', login);
$('clearTokenButton').addEventListener('click', () => {
  accessToken = '';
  if (hubConnection) {
    hubConnection.stop();
    hubConnection = null;
  }
  $('tokenStatus').textContent = 'Sem token em memoria.';
  show(0, 'Token removido da memoria.');
  setLiveStatus('SignalR desconectado.');
});
$('createTripButton').addEventListener('click', createTrip);
$('getTripButton').addEventListener('click', getTrip);
$('requestDispatchButton').addEventListener('click', requestDispatch);
$('acceptTripButton').addEventListener('click', acceptTrip);
$('startTripButton').addEventListener('click', startTrip);
$('finishTripButton').addEventListener('click', finishTrip);

document.querySelectorAll('[data-list]').forEach((button) => {
  button.addEventListener('click', () => list(button.dataset.list));
});

initMap();
