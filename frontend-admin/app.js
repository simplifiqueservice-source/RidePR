let accessToken = '';
let hubConnection = null;
let map = null;
let originMarker = null;
let destinationMarker = null;
let driverMarker = null;
let currentTrip = null;
let activePanel = 'dashboard';
let tripsCache = [];
let driversCache = [];
let passengersCache = [];
let vehiclesCache = [];

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

function statusLabel(status) {
  const normalized = String(status ?? '');

  return {
    0: 'Solicitada',
    1: 'Aceita',
    2: 'Em andamento',
    3: 'Finalizada',
    Requested: 'Solicitada',
    Accepted: 'Aceita',
    InProgress: 'Em andamento',
    Finished: 'Finalizada',
  }[normalized] ?? (normalized || 'Desconhecido');
}

function eventLabel(eventName) {
  return {
    TripRequested: 'Corrida solicitada',
    TripAccepted: 'Corrida aceita',
    TripStarted: 'Corrida iniciada',
    TripFinished: 'Corrida finalizada',
    DriverLocationUpdated: 'Localizacao do motorista atualizada',
    'Status consultado': 'Status consultado',
  }[eventName] ?? eventName;
}

function setLiveStatus(message) {
  $('liveStatus').textContent = message;
}

function setCurrentTripSummary(trip) {
  currentTrip = trip;
  updateDashboard();
}

function setButtonStates() {
  const hasToken = Boolean(accessToken);
  const hasTripId = Boolean($('tripId').value.trim());
  const hasDriverId = Boolean($('driverId').value.trim());

  $('createTripButton').disabled = !hasToken;
  $('getTripButton').disabled = !hasToken || !hasTripId;
  $('requestDispatchButton').disabled = !hasToken || !hasTripId;
  $('acceptTripButton').disabled = !hasToken || !hasTripId || !hasDriverId;
  $('startTripButton').disabled = !hasToken || !hasTripId || !hasDriverId;
  $('finishTripButton').disabled = !hasToken || !hasTripId || !hasDriverId;
}

function showPanel(panelName) {
  activePanel = panelName;
  const labels = {
    dashboard: 'Dashboard',
    corridas: 'Corridas',
    mapa: 'Mapa',
    motoristas: 'Motoristas',
    passageiros: 'Passageiros',
    veiculos: 'Veiculos',
    config: 'Configuracoes',
  };
  $('pageTitle').textContent = labels[panelName] ?? 'RidePR';
  document.querySelectorAll('[data-panel]').forEach((panel) => {
    const names = panel.dataset.panel.split(/\s+/);
    panel.classList.toggle('hidden', !names.includes(panelName));
  });
  document.querySelectorAll('[data-panel-target]').forEach((button) => {
    button.classList.toggle('active', button.dataset.panelTarget === panelName);
  });

  setTimeout(() => {
    if (map) {
      map.invalidateSize();
      fitMap();
    }
  }, 50);
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

function fitMapOnce() {
  if (!map || map._ridePrFitted) {
    return;
  }

  fitMap();
  map._ridePrFitted = true;
}

function updateTripMap(trip, eventName = 'Corrida atualizada') {
  const previousTripId = field(currentTrip, 'Id');
  const nextTripId = field(trip, 'Id');
  currentTrip = trip;
  initMap();

  if (map && previousTripId !== nextTripId) {
    map._ridePrFitted = false;
  }

  const originLatitude = Number(field(trip, 'OriginLatitude'));
  const originLongitude = Number(field(trip, 'OriginLongitude'));
  const destinationLatitude = Number(field(trip, 'DestinationLatitude'));
  const destinationLongitude = Number(field(trip, 'DestinationLongitude'));
  const status = statusLabel(field(trip, 'Status'));
  const tripId = field(trip, 'Id');

  originMarker = markerAt(originMarker, originLatitude, originLongitude, 'Origem');
  destinationMarker = markerAt(destinationMarker, destinationLatitude, destinationLongitude, 'Destino');
  fitMapOnce();
  setCurrentTripSummary(trip);
  setLiveStatus(`${eventLabel(eventName)}: ${status}${tripId ? ` (${tripId})` : ''}`);
  setButtonStates();
}

function updateDriverLocation(location) {
  initMap();

  const latitude = Number(field(location, 'Latitude'));
  const longitude = Number(field(location, 'Longitude'));
  const driverId = field(location, 'DriverId') ?? $('driverId').value.trim();

  driverMarker = markerAt(driverMarker, latitude, longitude, `Motorista ${driverId}`);
  setLiveStatus(`${eventLabel('DriverLocationUpdated')}: ${driverId}`);
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
    $('tokenStatus').textContent = 'Conectado';
    document.body.classList.remove('auth-locked');
    showPanel('dashboard');
    await connectRealtime();
    await refreshActivePanel();
    setButtonStates();
  }
}

async function list(path) {
  const { response, body } = await request(path);

  if (!response?.ok) {
    return;
  }

  if (path.startsWith('/api/drivers')) {
    renderDrivers(body);
  }

  if (path.startsWith('/api/vehicles')) {
    renderVehicles(body);
  }

  if (path.startsWith('/api/passengers')) {
    renderPassengers(body);
  }

  if (path.startsWith('/api/trips')) {
    renderTrips(body);
  }
}

async function refreshTripsQuietly() {
  if (!accessToken) {
    return;
  }

  const { response, body } = await request('/api/trips');

  if (response?.ok) {
    renderTrips(body);
  }
}

function pageItems(body) {
  return body?.items ?? body?.Items ?? (Array.isArray(body) ? body : []);
}

function cell(value) {
  return value === null || value === undefined || value === '' ? '-' : String(value);
}

function renderDrivers(body) {
  const rows = pageItems(body);
  driversCache = rows;
  const table = $('driversTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="7">Nenhum motorista encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((driver) => `
    <tr>
      <td>${cell(field(driver, 'Name'))}<br><span class="muted">${cell(field(driver, 'Email'))}</span></td>
      <td>${cell(field(driver, 'Phone'))}</td>
      <td>${cell(field(driver, 'Cpf'))}</td>
      <td>${cell(field(driver, 'Cnh'))} / ${cell(field(driver, 'CnhCategory'))}</td>
      <td>${driverStatusLabel(field(driver, 'Status'))}</td>
      <td>${approvalStatusLabel(field(driver, 'ApprovalStatus'))}</td>
      <td>${field(driver, 'Active') ? 'Sim' : 'Nao'}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderVehicles(body) {
  const rows = pageItems(body);
  vehiclesCache = rows;
  const table = $('vehiclesTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="7">Nenhum veiculo encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((vehicle) => `
    <tr>
      <td>${cell(field(vehicle, 'DriverName'))}</td>
      <td>${cell(field(vehicle, 'Plate'))}</td>
      <td>${cell(field(vehicle, 'Brand'))}</td>
      <td>${cell(field(vehicle, 'Model'))}</td>
      <td>${cell(field(vehicle, 'Year'))}</td>
      <td>${cell(field(vehicle, 'Color'))}</td>
      <td>${field(vehicle, 'Active') ? 'Sim' : 'Nao'}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderPassengers(body) {
  const rows = pageItems(body);
  passengersCache = rows;
  const table = $('passengersTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="6">Nenhum passageiro encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((passenger) => `
    <tr>
      <td>${cell(field(passenger, 'Name'))}<br><span class="muted">${cell(field(passenger, 'Email'))}</span></td>
      <td>${cell(field(passenger, 'Phone'))}</td>
      <td>${cell(field(passenger, 'Cpf'))}</td>
      <td>${cell(field(passenger, 'Address'))}<br><span class="muted">CEP ${cell(field(passenger, 'ZipCode'))}</span></td>
      <td>${cell(field(passenger, 'City'))}/${cell(field(passenger, 'State'))}</td>
      <td>${field(passenger, 'Active') ? 'Sim' : 'Nao'}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderTrips(body) {
  const rows = pageItems(body);
  tripsCache = rows;
  const table = $('tripsTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="6">Nenhuma corrida encontrada.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((trip) => `
    <tr>
      <td>${statusLabel(field(trip, 'Status'))}</td>
      <td>${cell(field(trip, 'Origin'))}</td>
      <td>${cell(field(trip, 'Destination'))}</td>
      <td>${cell(field(trip, 'PassengerName') ?? field(trip, 'PassengerId'))}</td>
      <td>${cell(field(trip, 'DriverId'))}</td>
      <td>R$ ${cell(field(trip, 'Price'))}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function upsertTrip(trip) {
  const tripId = cell(field(trip, 'Id'));
  const next = tripsCache.filter((item) => cell(field(item, 'Id')) !== tripId);
  renderTrips([trip, ...next]);
}

function updateDashboard() {
  const today = new Date().toISOString().slice(0, 10);
  const waiting = tripsCache.filter((trip) => statusLabel(field(trip, 'Status')) === 'Solicitada').length;
  const active = tripsCache.filter((trip) => ['Aceita', 'Em andamento'].includes(statusLabel(field(trip, 'Status')))).length;
  const finishedToday = tripsCache.filter((trip) => {
    const status = statusLabel(field(trip, 'Status'));
    const createdAt = String(field(trip, 'CreatedAt') ?? '');
    return status === 'Finalizada' && createdAt.startsWith(today);
  }).length;
  const onlineDrivers = driversCache.filter((driver) => driverStatusLabel(field(driver, 'Status')) === 'Online').length;

  $('waitingTripsMetric').textContent = waiting;
  $('activeTripsMetric').textContent = active;
  $('onlineDriversMetric').textContent = onlineDrivers;
  $('passengersMetric').textContent = passengersCache.length;
  $('finishedTodayMetric').textContent = finishedToday;
  $('lastUpdateMetric').textContent = new Date().toLocaleTimeString('pt-BR', {
    hour: '2-digit',
    minute: '2-digit',
  });
}

async function refreshActivePanel() {
  if (!accessToken) {
    show('LOCAL', 'Faca login antes de atualizar.');
    return;
  }

  if (activePanel === 'dashboard') {
    await Promise.all([
      list('/api/trips'),
      list('/api/drivers'),
      list('/api/passengers'),
    ]);
    return;
  }

  if (activePanel === 'motoristas') {
    await list('/api/drivers');
    return;
  }

  if (activePanel === 'passageiros') {
    await list('/api/passengers');
    return;
  }

  if (activePanel === 'veiculos') {
    await list('/api/vehicles');
    return;
  }

  await list('/api/trips');
}

function driverStatusLabel(status) {
  return {
    1: 'Offline',
    2: 'Online',
    3: 'Ocupado',
    4: 'Pausado',
    Offline: 'Offline',
    Online: 'Online',
    Busy: 'Ocupado',
    Paused: 'Pausado',
  }[String(status)] ?? cell(status);
}

function approvalStatusLabel(status) {
  return {
    0: 'Pendente',
    1: 'Em analise',
    2: 'Aprovado',
    3: 'Rejeitado',
    Pending: 'Pendente',
    UnderReview: 'Em analise',
    Approved: 'Aprovado',
    Rejected: 'Rejeitado',
  }[String(status)] ?? cell(status);
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
    upsertTrip(body);
    setButtonStates();
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

  const hubUrl = `${baseUrl()}/driverHub`;

  if (hubConnection && hubConnection.baseUrl === hubUrl) {
    if (hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }
    await hubConnection.start();
    setLiveStatus('SignalR conectado.');
    return;
  }

  if (hubConnection) {
    await hubConnection.stop();
  }

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: () => accessToken,
    })
    .withAutomaticReconnect()
    .build();

  const onTrip = (eventName) => (trip) => {
    updateTripMap(trip, eventName);
    upsertTrip(trip);
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
  hubConnection.baseUrl = hubUrl;
  setLiveStatus('SignalR conectado.');
}

$('loginButton').addEventListener('click', login);
$('clearTokenButton').addEventListener('click', () => {
  accessToken = '';
  if (hubConnection) {
    hubConnection.stop();
    hubConnection = null;
  }
  $('tokenStatus').textContent = 'Sem login';
  document.body.classList.add('auth-locked');
  setCurrentTripSummary(null);
  show(0, 'Token removido da memoria.');
  setLiveStatus('SignalR desconectado.');
  setButtonStates();
});
$('createTripButton').addEventListener('click', createTrip);
$('getTripButton').addEventListener('click', getTrip);
$('requestDispatchButton').addEventListener('click', requestDispatch);
$('acceptTripButton').addEventListener('click', acceptTrip);
$('startTripButton').addEventListener('click', startTrip);
$('finishTripButton').addEventListener('click', finishTrip);
$('refreshAllButton').addEventListener('click', refreshActivePanel);

document.querySelectorAll('[data-list]').forEach((button) => {
  button.addEventListener('click', () => list(button.dataset.list));
});

document.querySelectorAll('[data-panel-target]').forEach((button) => {
  button.addEventListener('click', () => showPanel(button.dataset.panelTarget));
});

['tripId', 'driverId'].forEach((id) => {
  $(id).addEventListener('input', setButtonStates);
});

initMap();
showPanel(activePanel);
setButtonStates();
