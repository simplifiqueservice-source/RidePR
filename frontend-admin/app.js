const adminTokenKey = 'rideprAdminAccessToken';

let accessToken = localStorage.getItem(adminTokenKey) || '';
let hubConnection = null;
let map = null;
let originMarker = null;
let destinationMarker = null;
let driverMarker = null;
let currentTrip = null;
let activePanel = 'corridas';
let tripsCache = [];
let driversCache = [];
let passengersCache = [];
let vehiclesCache = [];
let branchesCache = [];
let adminsCache = [];
let faresCache = [];

const $ = (id) => document.getElementById(id);

function baseUrl() {
  return $('baseUrl').value.trim().replace(/\/+$/, '');
}

function localApiBaseUrl() {
  const { protocol, hostname, port, origin } = window.location;

  if (protocol === 'http:' || protocol === 'https:') {
    if (port === '8282' || hostname === '127.0.0.1' || hostname === 'localhost') {
      return origin;
    }
  }

  return 'http://127.0.0.1:8282';
}

function isLocalPanel() {
  return ['127.0.0.1', 'localhost'].includes(window.location.hostname) || window.location.port === '8282';
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

function boolValue(id) {
  return $(id).value === 'true';
}

function show(statusCode, body) {
  const payload = typeof body === 'string' ? body : JSON.stringify(body, null, 2);
  $('responseBox').textContent = `HTTP ${statusCode}\n${payload || '(no body)'}`;
}

function statusLabel(status) {
  const normalized = String(status ?? '');

  return {
    0: 'Aguardando motorista',
    1: 'Aceita',
    2: 'Em andamento',
    3: 'Finalizada',
    4: 'Cancelada',
    Requested: 'Aguardando motorista',
    Dispatched: 'Enviada ao motorista',
    Accepted: 'Aceita',
    Started: 'Em andamento',
    InProgress: 'Em andamento',
    Finished: 'Finalizada',
    Cancelled: 'Cancelada',
    Rejected: 'Recusada',
    Active: 'Ativo',
    Inactive: 'Inativo',
    Pending: 'Pendente',
    Approved: 'Aprovado',
    Blocked: 'Bloqueado',
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
  const status = $('liveStatus');
  const normalized = String(message).toLowerCase();
  status.classList.remove('online', 'reconnecting', 'offline');

  if (normalized.includes('online') || normalized.includes('conectado')) {
    status.textContent = 'Online';
    status.classList.add('online');
    return;
  }

  if (normalized.includes('reconect')) {
    status.textContent = 'Reconectando';
    status.classList.add('reconnecting');
    return;
  }

  status.textContent = 'Offline';
  status.classList.add('offline');
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
    admins: 'Admins',
    filiais: 'Filiais',
    tarifas: 'Tarifas/Valores',
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

  if (accessToken) {
    refreshActivePanel();
  }

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

async function put(path, body) {
  return request(path, {
    method: 'PUT',
    body: JSON.stringify(body),
  });
}

async function remove(path) {
  return request(path, {
    method: 'DELETE',
  });
}

async function login() {
  $('tokenStatus').textContent = 'Entrando...';
  const { response, body } = await post('/api/auth/login', {
    email: $('email').value.trim(),
    password: $('password').value,
  });

  if (response?.ok && body?.accessToken) {
    accessToken = body.accessToken;
    localStorage.setItem(adminTokenKey, accessToken);
    $('tokenStatus').textContent = 'Conectado';
    $('loggedUser').textContent = body.name || body.email || 'Admin';
    document.body.classList.remove('auth-locked');
    showPanel('corridas');
    await connectRealtime();
    await refreshActivePanel();
    setButtonStates();
    return;
  }

  accessToken = '';
  localStorage.removeItem(adminTokenKey);
  document.body.classList.add('auth-locked');
  const message = body?.message ?? body ?? 'Nao foi possivel fazer login.';
  $('tokenStatus').textContent = response
    ? `Falha no login: HTTP ${response.status}`
    : 'Falha no login';
  show(response?.status ?? 'LOCAL', message);
}

function formatDate(value) {
  if (!value) return '-';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? cell(value)
    : date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function currency(value) {
  const number = Number(value || 0);
  return number.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function detailItem(label, value) {
  return `<div class="detail-item"><strong>${escapeHtml(label)}</strong><span>${escapeHtml(value)}</span></div>`;
}

function openDetails(title, items, technicalItems = []) {
  $('detailsTitle').textContent = title;
  $('detailsContent').innerHTML = items.map(([label, value]) => detailItem(label, value)).join('') +
    `<div class="detail-item"><strong>Detalhes tecnicos</strong><details><summary>Mostrar IDs</summary>${technicalItems
      .map(([label, value]) => `<p><strong>${escapeHtml(label)}:</strong> ${escapeHtml(value)}</p>`)
      .join('')}</details></div>`;
  $('detailsModal').showModal();
}

function focusTripOnMap(trip) {
  initMap();
  updateTripMap({
    id: field(trip, 'Id'),
    status: field(trip, 'Status'),
    originLatitude: field(trip, 'OriginLatitude'),
    originLongitude: field(trip, 'OriginLongitude'),
    destinationLatitude: field(trip, 'DestinationLatitude'),
    destinationLongitude: field(trip, 'DestinationLongitude'),
  }, 'Status consultado');
}

function showTripDetails(id) {
  const trip = tripsCache.find((item) => String(field(item, 'Id')) === String(id));
  if (!trip) return;

  focusTripOnMap(trip);
  openDetails(`Corrida ${field(trip, 'ShortCode') || ''}`, [
    ['Status', field(trip, 'StatusLabel')],
    ['Passageiro', `${field(trip, 'PassengerName')} - ${field(trip, 'PassengerPhone') || 'sem telefone'}`],
    ['Motorista', `${field(trip, 'DriverName')} - ${field(trip, 'DriverPhone') || 'sem telefone'}`],
    ['Veiculo', field(trip, 'Vehicle')],
    ['Origem completa', field(trip, 'OriginAddress') || field(trip, 'Origin')],
    ['Destino completo', field(trip, 'DestinationAddress') || field(trip, 'Destination')],
    ['Filial', field(trip, 'BranchName')],
    ['Valor', currency(field(trip, 'FareAmount') ?? field(trip, 'Price'))],
    ['Criada em', formatDate(field(trip, 'CreatedAt'))],
  ], [
    ['Corrida ID', field(trip, 'Id')],
    ['Passageiro ID', field(trip, 'PassengerId')],
    ['Motorista ID', field(trip, 'DriverId')],
    ['Filial ID', field(trip, 'BranchId')],
  ]);
}

function showEntityDetails(kind, id) {
  const caches = {
    driver: driversCache,
    passenger: passengersCache,
    vehicle: vehiclesCache,
  };
  const item = caches[kind].find((entry) => String(field(entry, 'Id')) === String(id));
  if (!item) return;

  const title = kind === 'driver' ? 'Motorista' : kind === 'passenger' ? 'Passageiro' : 'Veiculo';
  openDetails(title, Object.entries({
    Nome: field(item, 'Name') || field(item, 'DriverName') || field(item, 'Plate'),
    Telefone: field(item, 'Phone'),
    CPF: field(item, 'Cpf'),
    CNH: field(item, 'Cnh'),
    Veiculo: field(item, 'Vehicle') || `${field(item, 'Brand')} ${field(item, 'Model')} - ${field(item, 'Plate')}`,
    Cidade: `${field(item, 'City') || '-'} / ${field(item, 'State') || '-'}`,
    Filial: field(item, 'BranchName'),
    Status: field(item, 'StatusLabel') || field(item, 'ActiveLabel'),
  }), [
    ['ID', field(item, 'Id')],
    ['Usuario ID', field(item, 'UserId')],
    ['Motorista ID', field(item, 'DriverId')],
    ['Filial ID', field(item, 'BranchId')],
  ]);
}

function editBranch(id) {
  const branch = branchesCache.find((item) => String(field(item, 'Id')) === String(id));

  if (!branch) return;

  $('branchId').value = field(branch, 'Id') ?? '';
  $('branchName').value = field(branch, 'Name') ?? '';
  $('branchCity').value = field(branch, 'City') ?? '';
  $('branchState').value = field(branch, 'State') ?? '';
  $('branchPhone').value = field(branch, 'Phone') ?? '';
  $('branchAddress').value = field(branch, 'Address') ?? '';
  $('branchActive').value = field(branch, 'Active') ? 'true' : 'false';
  $('branchName').focus();
}

async function toggleBranch(id) {
  const branch = branchesCache.find((item) => String(field(item, 'Id')) === String(id));

  if (!branch) return;

  const nextActive = !field(branch, 'Active');

  if (!nextActive && !confirm('Tem certeza que deseja desativar esta filial?')) {
    return;
  }

  await put(`/api/branches/${id}`, {
    name: field(branch, 'Name'),
    city: field(branch, 'City'),
    state: field(branch, 'State'),
    address: field(branch, 'Address'),
    phone: field(branch, 'Phone'),
    active: nextActive,
  });
  await list('/api/branches');
}

function editAdmin(id) {
  const admin = adminsCache.find((item) => String(field(item, 'Id')) === String(id));

  if (!admin) return;

  $('adminId').value = field(admin, 'Id') ?? '';
  $('adminName').value = field(admin, 'Name') ?? '';
  $('adminEmail').value = field(admin, 'Email') ?? '';
  $('adminPassword').value = '';
  $('adminType').value = String(field(admin, 'AdminType')).includes('Filial') ? '2' : '1';
  $('adminBranch').value = field(admin, 'BranchId') ?? '';
  $('adminActive').value = field(admin, 'Active') ? 'true' : 'false';
  $('adminName').focus();
}

async function toggleAdmin(id) {
  const admin = adminsCache.find((item) => String(field(item, 'Id')) === String(id));

  if (!admin) return;

  const nextActive = !field(admin, 'Active');

  if (!nextActive && !confirm('Tem certeza que deseja desativar este admin?')) {
    return;
  }

  await put(`/api/admin-users/${id}`, {
    name: field(admin, 'Name'),
    email: field(admin, 'Email'),
    password: '',
    adminType: String(field(admin, 'AdminType')).includes('Filial') ? 2 : 1,
    branchId: field(admin, 'BranchId') || null,
    active: nextActive,
  });
  await list('/api/admin-users');
}

async function runAdminAction(action, id) {
  const actions = {
    'toggle-trip-details': () => showTripDetails(id),
    'toggle-driver-details': () => showEntityDetails('driver', id),
    'toggle-passenger-details': () => showEntityDetails('passenger', id),
    'toggle-vehicle-details': () => showEntityDetails('vehicle', id),
    'cancel-trip': async () => {
      if (!confirm('Tem certeza que deseja cancelar esta corrida?')) return;
      await post(`/api/trips/${id}/cancel`, {});
      await list('/api/admin/trips');
    },
    'finish-trip-admin': async () => {
      if (!confirm('Tem certeza que deseja finalizar esta corrida manualmente?')) return;
      await post(`/api/admin/trips/${id}/finish`, {});
      await list('/api/admin/trips');
    },
    'redispatch-trip': async () => {
      if (!confirm('Tem certeza que deseja reenviar esta corrida para motoristas?')) return;
      await post(`/api/admin/trips/${id}/redispatch`, {});
      await list('/api/admin/trips');
    },
    'approve-passenger': async () => {
      await post(`/api/admin/passengers/${id}/approve`, {});
      await list('/api/admin/passengers');
    },
    'block-passenger': async () => {
      if (!confirm('Tem certeza que deseja bloquear este passageiro?')) return;
      await post(`/api/admin/passengers/${id}/block`, {});
      await list('/api/admin/passengers');
    },
    'delete-passenger': async () => {
      if (!confirm('Tem certeza que deseja excluir este passageiro?')) return;
      await remove(`/api/admin/passengers/${id}`);
      await list('/api/admin/passengers');
    },
    'approve-driver': async () => {
      await post(`/api/admin/drivers/${id}/approve`, {});
      await list('/api/admin/drivers');
    },
    'block-driver': async () => {
      if (!confirm('Tem certeza que deseja bloquear este motorista?')) return;
      await post(`/api/admin/drivers/${id}/block`, {});
      await list('/api/admin/drivers');
    },
    'delete-driver': async () => {
      if (!confirm('Tem certeza que deseja excluir este motorista?')) return;
      await remove(`/api/admin/drivers/${id}`);
      await list('/api/admin/drivers');
    },
    'approve-vehicle': async () => {
      await post(`/api/admin/vehicles/${id}/approve`, {});
      await list('/api/admin/vehicles');
    },
    'disable-vehicle': async () => {
      if (!confirm('Tem certeza que deseja desativar este veiculo?')) return;
      await post(`/api/admin/vehicles/${id}/disable`, {});
      await list('/api/admin/vehicles');
    },
    'delete-vehicle': async () => {
      if (!confirm('Tem certeza que deseja excluir este veiculo?')) return;
      await remove(`/api/admin/vehicles/${id}`);
      await list('/api/admin/vehicles');
    },
    'edit-branch': () => editBranch(id),
    'toggle-branch': () => toggleBranch(id),
    'edit-admin': () => editAdmin(id),
    'toggle-admin': () => toggleAdmin(id),
  };

  await actions[action]?.();
}

async function list(path) {
  if (path === '/api/admin/trips') {
    const params = new URLSearchParams();
    const view = $('tripViewFilter')?.value || 'operational';
    const status = $('tripStatusFilter')?.value;
    const branchId = $('tripBranchFilter')?.value;

    if (view) params.set('view', view);
    if (status) params.set('status', status);
    if (branchId) params.set('branchId', branchId);
    path = `${path}${params.toString() ? `?${params}` : ''}`;
  }

  const { response, body } = await request(path);

  if (!response?.ok) {
    return;
  }

  if (path.startsWith('/api/admin/drivers') || path.startsWith('/api/drivers')) {
    renderDrivers(body);
  }

  if (path.startsWith('/api/admin/vehicles') || path.startsWith('/api/vehicles')) {
    renderVehicles(body);
  }

  if (path.startsWith('/api/admin/passengers') || path.startsWith('/api/passengers')) {
    renderPassengers(body);
  }

  if (path.startsWith('/api/admin/trips') || path.startsWith('/api/trips')) {
    renderTrips(body);
  }

  if (path.startsWith('/api/branches')) {
    renderBranches(body);
  }

  if (path.startsWith('/api/admin-users')) {
    renderAdmins(body);
  }

  if (path.startsWith('/api/branch-fares')) {
    renderFares(body);
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

function escapeHtml(value) {
  return cell(value)
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&#39;');
}

function fieldCell(source, name) {
  return escapeHtml(field(source, name));
}

function technicalDetails(label, item, colspan) {
  const id = escapeHtml(field(item, 'Id'));
  const passengerId = escapeHtml(field(item, 'PassengerId'));
  const driverId = escapeHtml(field(item, 'DriverId'));
  const userId = escapeHtml(field(item, 'UserId'));
  const branchId = escapeHtml(field(item, 'BranchId'));
  const technical = [
    id !== '-' ? `<span><strong>ID:</strong> ${id}</span>` : '',
    passengerId !== '-' ? `<span><strong>Passageiro ID:</strong> ${passengerId}</span>` : '',
    driverId !== '-' ? `<span><strong>Motorista ID:</strong> ${driverId}</span>` : '',
    userId !== '-' ? `<span><strong>Usuario ID:</strong> ${userId}</span>` : '',
    branchId !== '-' ? `<span><strong>Filial ID:</strong> ${branchId}</span>` : '',
  ].filter(Boolean).join('');

  return `
    <tr class="details-row hidden" data-details-row="${label}-${id}">
      <td colspan="${colspan}">
        <details>
          <summary>Detalhes tecnicos</summary>
          <div class="details-grid">${technical || '<span>Sem dados tecnicos.</span>'}</div>
        </details>
      </td>
    </tr>
  `;
}

function actionButton(label, action, id, className = 'ghost') {
  return `<button type="button" class="${className}" data-admin-action="${action}" data-id="${escapeHtml(id)}">${label}</button>`;
}

function rowActions(buttons) {
  return `<div class="row-actions">${buttons.join('')}</div>`;
}

function renderDrivers(body) {
  const rows = pageItems(body);
  driversCache = rows;
  const table = $('driversTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="8">Nenhum motorista encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((driver) => `
    <tr>
      <td>${fieldCell(driver, 'Name')}<br><span class="muted">${fieldCell(driver, 'Email')}</span></td>
      <td>${fieldCell(driver, 'Phone')}</td>
      <td>${fieldCell(driver, 'Cpf')}</td>
      <td>${fieldCell(driver, 'Cnh')} / ${fieldCell(driver, 'CnhCategory')}<br><span class="muted">${fieldCell(driver, 'Vehicle')}</span></td>
      <td>${escapeHtml(field(driver, 'StatusLabel') ?? driverStatusLabel(field(driver, 'Status')))}</td>
      <td>${escapeHtml(field(driver, 'ApprovalStatusLabel') ?? approvalStatusLabel(field(driver, 'ApprovalStatus')))}</td>
      <td>${escapeHtml(field(driver, 'ActiveLabel') ?? (field(driver, 'Active') ? 'Ativo' : 'Bloqueado'))}</td>
      <td>${rowActions([
        actionButton('Ver detalhes', 'toggle-driver-details', field(driver, 'Id')),
        actionButton('Aprovar', 'approve-driver', field(driver, 'Id')),
        actionButton('Bloquear', 'block-driver', field(driver, 'Id'), 'danger'),
        actionButton('Excluir', 'delete-driver', field(driver, 'Id'), 'danger'),
      ])}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderVehicles(body) {
  const rows = pageItems(body);
  vehiclesCache = rows;
  const table = $('vehiclesTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="8">Nenhum veiculo encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((vehicle) => `
    <tr>
      <td>${fieldCell(vehicle, 'DriverName')}<br><span class="muted">${fieldCell(vehicle, 'BranchName')}</span></td>
      <td>${fieldCell(vehicle, 'Plate')}</td>
      <td>${fieldCell(vehicle, 'Brand')}</td>
      <td>${fieldCell(vehicle, 'Model')}</td>
      <td>${fieldCell(vehicle, 'Year')}</td>
      <td>${fieldCell(vehicle, 'Color')}</td>
      <td>${escapeHtml(field(vehicle, 'StatusLabel') ?? (field(vehicle, 'Active') ? 'Ativo' : 'Inativo'))}</td>
      <td>${rowActions([
        actionButton('Ver detalhes', 'toggle-vehicle-details', field(vehicle, 'Id')),
        actionButton('Aprovar', 'approve-vehicle', field(vehicle, 'Id')),
        actionButton('Desativar', 'disable-vehicle', field(vehicle, 'Id'), 'danger'),
        actionButton('Excluir', 'delete-vehicle', field(vehicle, 'Id'), 'danger'),
      ])}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderPassengers(body) {
  const rows = pageItems(body);
  passengersCache = rows;
  const table = $('passengersTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="7">Nenhum passageiro encontrado.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((passenger) => `
    <tr>
      <td>${fieldCell(passenger, 'Name')}<br><span class="muted">${fieldCell(passenger, 'Email')}</span></td>
      <td>${fieldCell(passenger, 'Phone')}</td>
      <td>${fieldCell(passenger, 'Cpf')}</td>
      <td>${fieldCell(passenger, 'Address')}<br><span class="muted">CEP ${fieldCell(passenger, 'ZipCode')}</span></td>
      <td>${fieldCell(passenger, 'City')}/${fieldCell(passenger, 'State')}</td>
      <td>${escapeHtml(field(passenger, 'StatusLabel') ?? (field(passenger, 'Active') ? 'Aprovado' : 'Bloqueado'))}</td>
      <td>${rowActions([
        actionButton('Ver detalhes', 'toggle-passenger-details', field(passenger, 'Id')),
        actionButton('Aprovar', 'approve-passenger', field(passenger, 'Id')),
        actionButton('Bloquear', 'block-passenger', field(passenger, 'Id'), 'danger'),
        actionButton('Excluir', 'delete-passenger', field(passenger, 'Id'), 'danger'),
      ])}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderTrips(body) {
  const rows = pageItems(body);
  tripsCache = rows;
  const table = $('tripsTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="9">Nenhuma corrida encontrada.</td></tr>';
    updateDashboard();
    return;
  }

  table.innerHTML = rows.map((trip) => `
    <tr>
      <td><strong>${fieldCell(trip, 'ShortCode')}</strong></td>
      <td>${escapeHtml(field(trip, 'StatusLabel') ?? statusLabel(field(trip, 'Status')))}</td>
      <td>${fieldCell(trip, 'PassengerName')}<br><span class="muted">${fieldCell(trip, 'PassengerPhone')}</span></td>
      <td>${fieldCell(trip, 'DriverName')}<br><span class="muted">${fieldCell(trip, 'Vehicle')}</span></td>
      <td>${fieldCell(trip, 'OriginShort')}</td>
      <td>${fieldCell(trip, 'DestinationShort')}</td>
      <td>${currency(field(trip, 'FareAmount') ?? field(trip, 'Price'))}</td>
      <td>${formatDate(field(trip, 'CreatedAt'))}</td>
      <td>${rowActions([
        actionButton('Ver detalhes', 'toggle-trip-details', field(trip, 'Id')),
        actionButton('Cancelar', 'cancel-trip', field(trip, 'Id'), 'danger'),
        actionButton('Finalizar', 'finish-trip-admin', field(trip, 'Id')),
        actionButton('Reenviar', 'redispatch-trip', field(trip, 'Id')),
      ])}</td>
    </tr>
  `).join('');
  updateDashboard();
}

function renderBranches(body) {
  const rows = pageItems(body);
  branchesCache = rows;
  const table = $('branchesTableBody');

  renderBranchOptions();

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="6">Nenhuma filial encontrada.</td></tr>';
    return;
  }

  table.innerHTML = rows.map((branch) => `
    <tr>
      <td>${fieldCell(branch, 'Name')}</td>
      <td>${fieldCell(branch, 'City')}/${fieldCell(branch, 'State')}</td>
      <td>${fieldCell(branch, 'Address')}</td>
      <td>${fieldCell(branch, 'Phone')}</td>
      <td>${field(branch, 'Active') ? 'Ativa' : 'Inativa'}</td>
      <td>${rowActions([
        actionButton('Editar', 'edit-branch', field(branch, 'Id')),
        actionButton(field(branch, 'Active') ? 'Desativar' : 'Ativar', 'toggle-branch', field(branch, 'Id'), field(branch, 'Active') ? 'danger' : 'ghost'),
      ])}</td>
    </tr>
  `).join('');
}

function renderBranchOptions() {
  const options = ['<option value="">Sem filial</option>']
    .concat(branchesCache.map((branch) => `<option value="${field(branch, 'Id')}">${cell(field(branch, 'Name'))}</option>`))
    .join('');

  const filterOptions = ['<option value="">Todas as filiais</option>']
    .concat(branchesCache.map((branch) => `<option value="${field(branch, 'Id')}">${cell(field(branch, 'Name'))}</option>`))
    .join('');

  ['adminBranch', 'fareBranch'].forEach((id) => {
    const select = $(id);
    if (select) select.innerHTML = options;
  });

  const tripBranchFilter = $('tripBranchFilter');
  if (tripBranchFilter) {
    const selected = tripBranchFilter.value;
    tripBranchFilter.innerHTML = filterOptions;
    tripBranchFilter.value = selected;
  }
}

function renderAdmins(body) {
  const rows = pageItems(body);
  adminsCache = rows;
  const table = $('adminsTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="6">Nenhum admin encontrado.</td></tr>';
    return;
  }

  table.innerHTML = rows.map((admin) => `
    <tr>
      <td>${fieldCell(admin, 'Name')}</td>
      <td>${fieldCell(admin, 'Email')}</td>
      <td>${fieldCell(admin, 'AdminType')}</td>
      <td>${fieldCell(admin, 'BranchName')}</td>
      <td>${field(admin, 'Active') ? 'Ativo' : 'Inativo'}</td>
      <td>${rowActions([
        actionButton('Editar', 'edit-admin', field(admin, 'Id')),
        actionButton(field(admin, 'Active') ? 'Desativar' : 'Ativar', 'toggle-admin', field(admin, 'Id'), field(admin, 'Active') ? 'danger' : 'ghost'),
      ])}</td>
    </tr>
  `).join('');
}

function renderFares(body) {
  const rows = pageItems(body);
  faresCache = rows;
  const table = $('faresTableBody');

  if (!rows.length) {
    table.innerHTML = '<tr><td colspan="6">Nenhuma tarifa encontrada.</td></tr>';
    return;
  }

  table.innerHTML = rows.map((fare) => `
    <tr>
      <td>${cell(field(fare, 'BranchName'))}</td>
      <td>R$ ${cell(field(fare, 'BaseFare'))}</td>
      <td>R$ ${cell(field(fare, 'PricePerKm'))}</td>
      <td>R$ ${cell(field(fare, 'PricePerMinute'))}</td>
      <td>R$ ${cell(field(fare, 'MinimumFare'))}</td>
      <td>${field(fare, 'Active') ? 'Ativa' : 'Inativa'}</td>
    </tr>
  `).join('');
}

function upsertTrip(trip) {
  const tripId = cell(field(trip, 'Id'));
  const next = tripsCache.filter((item) => cell(field(item, 'Id')) !== tripId);
  renderTrips([trip, ...next]);
}

function updateDashboard() {
  const today = new Date().toISOString().slice(0, 10);
  const waiting = tripsCache.filter((trip) => statusLabel(field(trip, 'Status')) === 'Aguardando motorista').length;
  const active = tripsCache.filter((trip) => ['Aceita', 'Em andamento'].includes(statusLabel(field(trip, 'Status')))).length;
  const finishedToday = tripsCache.filter((trip) => {
    const status = statusLabel(field(trip, 'Status'));
    const createdAt = String(field(trip, 'CreatedAt') ?? '');
    return status === 'Finalizada' && createdAt.startsWith(today);
  }).length;
  const onlineDrivers = driversCache.filter((driver) => {
    const status = field(driver, 'Status');
    return status === 'Online' || status === 2 || status === '2';
  }).length;
  const revenueToday = tripsCache
    .filter((trip) => String(field(trip, 'CreatedAt') ?? '').startsWith(today))
    .reduce((total, trip) => total + Number(field(trip, 'FareAmount') ?? field(trip, 'Price') ?? 0), 0);

  $('waitingTripsMetric').textContent = waiting;
  $('activeTripsMetric').textContent = active;
  $('onlineDriversMetric').textContent = onlineDrivers;
  $('passengersMetric').textContent = passengersCache.length;
  $('finishedTodayMetric').textContent = finishedToday;
  $('revenueTodayMetric').textContent = currency(revenueToday);
}

async function refreshActivePanel() {
  if (!accessToken) {
    show('LOCAL', 'Faca login antes de atualizar.');
    return;
  }

  if (activePanel === 'dashboard') {
    await Promise.all([
      list('/api/admin/trips'),
      list('/api/admin/drivers'),
      list('/api/admin/passengers'),
      list('/api/branches'),
    ]);
    return;
  }

  if (activePanel === 'motoristas') {
    await list('/api/admin/drivers');
    return;
  }

  if (activePanel === 'passageiros') {
    await list('/api/admin/passengers');
    return;
  }

  if (activePanel === 'veiculos') {
    await list('/api/admin/vehicles');
    return;
  }

  if (activePanel === 'admins') {
    await list('/api/admin-users');
    await list('/api/branches');
    return;
  }

  if (activePanel === 'filiais') {
    await list('/api/branches');
    return;
  }

  if (activePanel === 'tarifas') {
    await list('/api/branches');
    await list('/api/branch-fares');
    return;
  }

  if (activePanel === 'corridas' || activePanel === 'mapa') {
    await list('/api/branches');
    await list('/api/admin/trips');
    return;
  }

  await list('/api/admin/trips');
}

function driverStatusLabel(status) {
  return {
    1: 'Inativo',
    2: 'Ativo',
    3: 'Em corrida',
    4: 'Pausado',
    Offline: 'Inativo',
    Online: 'Ativo',
    Busy: 'Em corrida',
    Paused: 'Pausado',
  }[String(status)] ?? cell(status);
}

function approvalStatusLabel(status) {
  return {
    0: 'Pendente',
    1: 'Pendente',
    2: 'Aprovado',
    3: 'Recusado',
    Pending: 'Pendente',
    Approved: 'Aprovado',
    Rejected: 'Recusado',
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

async function saveBranch() {
  const branchId = $('branchId').value.trim();
  const payload = {
    name: $('branchName').value.trim(),
    city: $('branchCity').value.trim(),
    state: $('branchState').value.trim(),
    address: $('branchAddress').value.trim(),
    phone: $('branchPhone').value.trim(),
    active: boolValue('branchActive'),
  };
  const { response } = branchId
    ? await put(`/api/branches/${branchId}`, payload)
    : await post('/api/branches', payload);

  if (response?.ok) {
    $('branchId').value = '';
    await list('/api/branches');
  }
}

async function saveAdmin() {
  const branchId = $('adminBranch').value || null;
  const adminId = $('adminId').value.trim();
  const payload = {
    name: $('adminName').value.trim(),
    email: $('adminEmail').value.trim(),
    password: $('adminPassword').value,
    adminType: Number.parseInt($('adminType').value, 10),
    branchId,
    active: boolValue('adminActive'),
  };
  const { response } = adminId
    ? await put(`/api/admin-users/${adminId}`, payload)
    : await post('/api/admin-users', payload);

  if (response?.ok) {
    $('adminId').value = '';
    $('adminPassword').value = '';
    await list('/api/admin-users');
  }
}

async function saveFare() {
  const branchId = $('fareBranch').value || null;
  const { response } = await post('/api/branch-fares', {
    branchId,
    name: 'Padrao',
    baseFare: numberValue('fareBase'),
    minimumFare: numberValue('fareMinimum'),
    pricePerKm: numberValue('fareKm'),
    pricePerMinute: numberValue('fareMinute'),
    cancellationFee: numberValue('fareCancellation'),
    active: true,
  });

  if (response?.ok) {
    await list('/api/branch-fares');
  }
}

async function connectRealtime() {
  if (!window.signalR) {
    setLiveStatus('Offline');
    return;
  }

  const hubUrl = `${baseUrl()}/driverHub`;

  if (hubConnection && hubConnection.baseUrl === hubUrl) {
    if (hubConnection.state === signalR.HubConnectionState.Connected) {
      return;
    }
    try {
      await hubConnection.start();
      setLiveStatus('Online');
    } catch (error) {
      setLiveStatus('Offline');
      show('SIGNALR', String(error));
    }
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

  hubConnection.onreconnecting(() => setLiveStatus('Reconectando'));
  hubConnection.onreconnected(() => setLiveStatus('Online'));
  hubConnection.onclose(() => setLiveStatus('Offline'));

  try {
    await hubConnection.start();
    hubConnection.baseUrl = hubUrl;
    setLiveStatus('Online');
  } catch (error) {
    setLiveStatus('Offline');
    show('SIGNALR', String(error));
  }
}

$('loginButton').addEventListener('click', login);
$('clearTokenButton').addEventListener('click', () => {
  accessToken = '';
  localStorage.removeItem(adminTokenKey);
  if (hubConnection) {
    hubConnection.stop();
    hubConnection = null;
  }
  $('tokenStatus').textContent = 'Sem login';
  document.body.classList.add('auth-locked');
  setCurrentTripSummary(null);
  show(0, 'Login removido deste navegador.');
  setLiveStatus('Offline');
  setButtonStates();
});
$('createTripButton').addEventListener('click', createTrip);
$('getTripButton').addEventListener('click', getTrip);
$('requestDispatchButton').addEventListener('click', requestDispatch);
$('acceptTripButton').addEventListener('click', acceptTrip);
$('startTripButton').addEventListener('click', startTrip);
$('finishTripButton').addEventListener('click', finishTrip);
$('refreshAllButton').addEventListener('click', refreshActivePanel);
$('cancelOldTripsButton').addEventListener('click', async () => {
  if (!confirm('Tem certeza que deseja cancelar corridas pendentes antigas?')) return;
  await post('/api/admin/trips/cancel-old-pending', {});
  await list('/api/admin/trips');
});
$('saveBranchButton').addEventListener('click', saveBranch);
$('saveAdminButton').addEventListener('click', saveAdmin);
$('saveFareButton').addEventListener('click', saveFare);

document.querySelectorAll('[data-list]').forEach((button) => {
  button.addEventListener('click', () => list(button.dataset.list));
});

document.querySelectorAll('[data-panel-target]').forEach((button) => {
  button.addEventListener('click', () => showPanel(button.dataset.panelTarget));
});

document.addEventListener('click', async (event) => {
  const button = event.target.closest('[data-admin-action]');

  if (!button) {
    return;
  }

  await runAdminAction(button.dataset.adminAction, button.dataset.id);
});

['tripViewFilter', 'tripStatusFilter', 'tripBranchFilter'].forEach((id) => {
  const filter = $(id);
  if (filter) {
    filter.addEventListener('change', () => list('/api/admin/trips'));
  }
});

$('closeDetailsButton').addEventListener('click', () => $('detailsModal').close());

['tripId', 'driverId'].forEach((id) => {
  $(id).addEventListener('input', setButtonStates);
});

async function bootstrapAdminPanel() {
  $('baseUrl').value = localApiBaseUrl();

  initMap();
  showPanel(activePanel);

  if (accessToken) {
    $('tokenStatus').textContent = 'Conectado';
    $('loggedUser').textContent = 'Admin';
    document.body.classList.remove('auth-locked');
    await connectRealtime();
    await refreshActivePanel();
  }

  setButtonStates();
}

bootstrapAdminPanel();
