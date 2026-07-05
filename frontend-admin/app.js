let accessToken = '';

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
  }
}

async function getTrip() {
  await request(`/api/trips/${$('tripId').value.trim()}`);
}

async function requestDispatch() {
  await post('/api/dispatch/request', {
    tripId: $('tripId').value.trim(),
    radiusKm: numberValue('radiusKm'),
    timeoutSeconds: 30,
    maxCandidates: 10,
  });
}

async function acceptTrip() {
  await post('/api/dispatch/accept', {
    tripId: $('tripId').value.trim(),
    driverId: $('driverId').value.trim(),
  });
}

async function startTrip() {
  await post(`/api/trips/${$('tripId').value.trim()}/start`, {
    driverId: $('driverId').value.trim(),
  });
}

async function finishTrip() {
  await post(`/api/trips/${$('tripId').value.trim()}/finish`, {
    driverId: $('driverId').value.trim(),
    actualDistanceKm: numberValue('actualDistanceKm'),
    actualDurationMinutes: numberValue('actualDurationMinutes'),
  });
}

$('loginButton').addEventListener('click', login);
$('clearTokenButton').addEventListener('click', () => {
  accessToken = '';
  $('tokenStatus').textContent = 'Sem token em memoria.';
  show(0, 'Token removido da memoria.');
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
