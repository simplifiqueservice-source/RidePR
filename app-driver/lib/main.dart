import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart';

void main() {
  runApp(const RidePrDriverApp());
}

class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'RIDEPR_API_URL',
    defaultValue: 'http://192.168.1.15:5090',
  );
}

class RidePrDriverApp extends StatefulWidget {
  const RidePrDriverApp({super.key});

  @override
  State<RidePrDriverApp> createState() => _RidePrDriverAppState();
}

class _RidePrDriverAppState extends State<RidePrDriverApp> {
  late final AuthSession session = AuthSession(ApiClient(AppConfig.apiBaseUrl));
  bool loading = true;

  @override
  void initState() {
    super.initState();
    session.restore().whenComplete(() => setState(() => loading = false));
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'RidePR Motorista',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff1d4ed8)),
        useMaterial3: true,
      ),
      home: loading
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : session.isAuthenticated
              ? DriverHomePage(session: session)
              : LoginPage(session: session, requiredRole: 'Driver'),
    );
  }
}

class ApiClient {
  ApiClient(this.baseUrl);

  String baseUrl;
  String? accessToken;

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final normalizedBase = baseUrl.trim().replaceAll(RegExp(r'/+$'), '');
    final normalizedPath = path.startsWith('/') ? path : '/$path';
    final values = query?.map((key, value) => MapEntry(key, '$value'));

    return Uri.parse('$normalizedBase$normalizedPath')
        .replace(queryParameters: values);
  }

  Future<dynamic> get(String path, [Map<String, dynamic>? query]) async {
    return _send(() => http.get(_uri(path, query), headers: _headers()));
  }

  Future<dynamic> post(
    String path,
    Map<String, dynamic> body, [
    Map<String, dynamic>? query,
  ]) async {
    return _send(
      () => http.post(
        _uri(path, query),
        headers: _headers(),
        body: jsonEncode(body),
      ),
    );
  }

  Future<dynamic> patch(String path, Map<String, dynamic> body) async {
    return _send(
      () => http.patch(
        _uri(path),
        headers: _headers(),
        body: jsonEncode(body),
      ),
    );
  }

  Future<dynamic> put(String path, Map<String, dynamic> body) async {
    return _send(
      () => http.put(
        _uri(path),
        headers: _headers(),
        body: jsonEncode(body),
      ),
    );
  }

  Map<String, String> _headers() {
    return {
      'Content-Type': 'application/json',
      if (accessToken != null && accessToken!.isNotEmpty)
        'Authorization': 'Bearer $accessToken',
    };
  }

  Future<dynamic> _send(Future<http.Response> Function() request) async {
    final response = await request();
    final text = response.body.trim();
    dynamic data;

    if (text.isNotEmpty) {
      try {
        data = jsonDecode(text);
      } catch (_) {
        data = text;
      }
    }

    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw ApiException(data is String ? data : 'Erro ${response.statusCode}');
    }

    return data;
  }
}

class ApiException implements Exception {
  ApiException(this.message);

  final String message;

  @override
  String toString() => message;
}

class AuthSession {
  AuthSession(this.api);

  final ApiClient api;
  String? userId;
  String? name;
  String? email;
  String? role;
  String? refreshToken;

  bool get isAuthenticated =>
      api.accessToken != null && api.accessToken!.isNotEmpty;

  Future<void> restore() async {
    final prefs = await SharedPreferences.getInstance();
    api.baseUrl = prefs.getString('baseUrl') ?? api.baseUrl;
    api.accessToken = prefs.getString('accessToken');
    refreshToken = prefs.getString('refreshToken');
    userId = prefs.getString('userId');
    name = prefs.getString('name');
    email = prefs.getString('email');
    role = prefs.getString('role');
  }

  Future<void> login(String baseUrl, String email, String password) async {
    api.baseUrl = baseUrl.trim().replaceAll(RegExp(r'/+$'), '');
    final data = await api.post('/api/auth/login', {
      'email': email,
      'password': password,
    }) as Map<String, dynamic>;

    api.accessToken = data['accessToken'] as String?;
    refreshToken = data['refreshToken'] as String?;
    userId = data['userId'] as String?;
    name = data['name'] as String?;
    this.email = data['email'] as String?;
    role = data['role'] as String?;

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('baseUrl', api.baseUrl);
    await prefs.setString('accessToken', api.accessToken ?? '');
    await prefs.setString('refreshToken', refreshToken ?? '');
    await prefs.setString('userId', userId ?? '');
    await prefs.setString('name', name ?? '');
    await prefs.setString('email', this.email ?? '');
    await prefs.setString('role', role ?? '');
  }

  Future<void> logout() async {
    api.accessToken = null;
    refreshToken = null;
    userId = null;
    name = null;
    email = null;
    role = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
  }
}

class LoginPage extends StatefulWidget {
  const LoginPage({
    required this.session,
    required this.requiredRole,
    super.key,
  });

  final AuthSession session;
  final String requiredRole;

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  late final baseUrl = TextEditingController(text: widget.session.api.baseUrl);
  final email = TextEditingController(text: 'motorista.mvp@ridepr.test');
  final password = TextEditingController(text: 'Senha123!');
  bool loading = false;
  String? error;

  @override
  void dispose() {
    baseUrl.dispose();
    email.dispose();
    password.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: ListView(
              shrinkWrap: true,
              padding: const EdgeInsets.all(24),
              children: [
                Text(
                  'RidePR',
                  style: Theme.of(context).textTheme.displaySmall?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                ),
                const SizedBox(height: 6),
                Text(
                  'App do motorista MVP',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 28),
                _Input(controller: baseUrl, label: 'API baseUrl'),
                _Input(controller: email, label: 'E-mail'),
                _Input(
                  controller: password,
                  label: 'Senha',
                  obscureText: true,
                ),
                if (error != null) ...[
                  const SizedBox(height: 8),
                  Text(error!, style: const TextStyle(color: Colors.red)),
                ],
                const SizedBox(height: 12),
                FilledButton.icon(
                  onPressed: loading ? null : _submit,
                  icon: const Icon(Icons.login),
                  label: Text(loading ? 'Entrando...' : 'Entrar'),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      await widget.session.login(
        baseUrl.text.trim(),
        email.text.trim(),
        password.text,
      );

      if (widget.session.role != widget.requiredRole &&
          widget.session.role != 'Administrator') {
        throw ApiException('Usuario sem perfil de motorista.');
      }

      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => DriverHomePage(session: widget.session),
        ),
      );
    } catch (ex) {
      setState(() => error = ex.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }
}

class DriverHomePage extends StatefulWidget {
  const DriverHomePage({required this.session, super.key});

  final AuthSession session;

  @override
  State<DriverHomePage> createState() => _DriverHomePageState();
}

class _DriverHomePageState extends State<DriverHomePage> {
  final latitude = TextEditingController(text: '-23.55052');
  final longitude = TextEditingController(text: '-46.63331');
  final speed = TextEditingController(text: '0');
  final heading = TextEditingController(text: '0');
  final actualDistance = TextEditingController(text: '4.2');
  final actualDuration = TextEditingController(text: '18');
  final cpf = TextEditingController();
  final rg = TextEditingController();
  final birthDate = TextEditingController(text: '1990-01-01');
  final phone = TextEditingController();
  final emergencyPhone = TextEditingController();
  final address = TextEditingController();
  final city = TextEditingController(text: 'Sao Paulo');
  final state = TextEditingController(text: 'SP');
  final zipCode = TextEditingController();
  final cnhNumber = TextEditingController();
  final cnhCategory = TextEditingController(text: 'B');
  final cnhExpiration = TextEditingController(text: '2030-01-01');
  final vehiclePlate = TextEditingController();
  final vehicleBrand = TextEditingController();
  final vehicleModel = TextEditingController();
  final vehicleColor = TextEditingController();
  final vehicleYear = TextEditingController(text: '2022');
  final vehicleRenavam = TextEditingController();
  final vehicleChassis = TextEditingController();

  HubConnection? hubConnection;
  Timer? locationTimer;
  Map<String, dynamic>? driver;
  Map<String, dynamic>? vehicle;
  Map<String, dynamic>? currentOffer;
  Map<String, dynamic>? activeTrip;
  List<Map<String, dynamic>> availableTrips = [];
  bool loading = true;
  bool locationStreaming = false;
  int selectedStatus = 2;
  String liveStatus = 'SignalR desconectado.';
  String lastEvent = 'Nenhum evento recebido ainda.';

  String? get driverId => driver?['id']?.toString();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    locationTimer?.cancel();
    hubConnection?.stop();
    latitude.dispose();
    longitude.dispose();
    speed.dispose();
    heading.dispose();
    actualDistance.dispose();
    actualDuration.dispose();
    cpf.dispose();
    rg.dispose();
    birthDate.dispose();
    phone.dispose();
    emergencyPhone.dispose();
    address.dispose();
    city.dispose();
    state.dispose();
    zipCode.dispose();
    cnhNumber.dispose();
    cnhCategory.dispose();
    cnhExpiration.dispose();
    vehiclePlate.dispose();
    vehicleBrand.dispose();
    vehicleModel.dispose();
    vehicleColor.dispose();
    vehicleYear.dispose();
    vehicleRenavam.dispose();
    vehicleChassis.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('RidePR Motorista'),
        actions: [
          IconButton(
            tooltip: 'Atualizar',
            onPressed: _load,
            icon: const Icon(Icons.refresh),
          ),
          IconButton(
            tooltip: 'Sair',
            onPressed: _logout,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 32),
                children: [
                  _HeaderCard(
                    title: widget.session.name ?? 'Motorista',
                    subtitle: driver == null
                        ? widget.session.email ?? ''
                        : 'Status: ${_driverStatusLabel(driver!['status'])}',
                    trailing: driver == null ? 'Sem cadastro' : liveStatus,
                  ),
                  const SizedBox(height: 16),
                  _DriverRegistrationCard(
                    hasDriver: driver != null,
                    cpf: cpf,
                    rg: rg,
                    birthDate: birthDate,
                    phone: phone,
                    emergencyPhone: emergencyPhone,
                    address: address,
                    city: city,
                    state: state,
                    zipCode: zipCode,
                    cnhNumber: cnhNumber,
                    cnhCategory: cnhCategory,
                    cnhExpiration: cnhExpiration,
                    active: _boolValue(driver, 'active', fallback: true),
                    onSave: _saveDriver,
                    onToggleActive: driver == null ? null : _toggleDriverActive,
                  ),
                  const SizedBox(height: 16),
                  _VehicleRegistrationCard(
                    hasDriver: driver != null,
                    hasVehicle: vehicle != null,
                    plate: vehiclePlate,
                    brand: vehicleBrand,
                    model: vehicleModel,
                    color: vehicleColor,
                    year: vehicleYear,
                    renavam: vehicleRenavam,
                    chassis: vehicleChassis,
                    active: _boolValue(vehicle, 'active', fallback: true),
                    onSave: driver == null ? null : _saveVehicle,
                    onToggleActive: vehicle == null ? null : _toggleVehicleActive,
                  ),
                  const SizedBox(height: 16),
                  _AvailabilityCard(
                    selectedStatus: selectedStatus,
                    onChanged: (value) =>
                        setState(() => selectedStatus = value ?? 2),
                    onUpdate: _updateStatus,
                  ),
                  const SizedBox(height: 16),
                  _OfferCard(
                    offer: currentOffer,
                    onAccept: currentOffer == null
                        ? null
                        : () => _acceptTrip(currentOffer!['tripId'].toString()),
                    onReject: currentOffer == null
                        ? null
                        : () => _rejectTrip(currentOffer!['tripId'].toString()),
                  ),
                  const SizedBox(height: 16),
                  _ActiveTripCard(
                    trip: activeTrip,
                    actualDistance: actualDistance,
                    actualDuration: actualDuration,
                    canStart: _tripStatusLabel(
                          activeTrip?['status'] ?? activeTrip?['Status'],
                        ) ==
                        'Accepted',
                    canFinish: _tripStatusLabel(
                          activeTrip?['status'] ?? activeTrip?['Status'],
                        ) ==
                        'InProgress',
                    onStart: activeTrip == null
                        ? null
                        : () => _startTrip(_id(activeTrip!)),
                    onFinish: activeTrip == null
                        ? null
                        : () => _finishTrip(_id(activeTrip!)),
                  ),
                  const SizedBox(height: 16),
                  _AvailableTripsCard(
                    trips: availableTrips,
                    onAccept: (trip) => _acceptTrip(_id(trip)),
                  ),
                  const SizedBox(height: 16),
                  _LocationCard(
                    latitude: latitude,
                    longitude: longitude,
                    speed: speed,
                    heading: heading,
                    streaming: locationStreaming,
                    onSend: _sendLocation,
                    onToggleStream: _toggleLocationStream,
                  ),
                  const SizedBox(height: 16),
                  _DebugCard(text: lastEvent),
                ],
              ),
            ),
    );
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      lastEvent = 'Carregando dados do motorista...';
    });

    try {
      final userId = widget.session.userId!;
      try {
        driver = await widget.session.api.get('/api/drivers/by-user/$userId')
            as Map<String, dynamic>;
        _fillDriverForm(driver!);
        selectedStatus = _driverStatusNumber(driver!['status']);
        await _loadVehicles();
        await _connectRealtime();
        await _loadTrips();
      } catch (ex) {
        driver = null;
        vehicle = null;
        availableTrips = [];
        activeTrip = null;
        lastEvent = 'Complete seu cadastro de motorista para ficar online.';
      }
    } catch (ex) {
      lastEvent = ex.toString();
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _loadTrips() async {
    if (driverId == null) {
      return;
    }

    final data = await widget.session.api.get('/api/trips');
    final list = data is List ? data : <dynamic>[];
    final trips = list
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .toList();

    setState(() {
      availableTrips = trips
          .where((trip) =>
              _tripStatusLabel(trip['status']) == 'Requested' &&
              (trip['driverId'] == null || '${trip['driverId']}'.isEmpty))
          .toList();
      activeTrip = trips.cast<Map<String, dynamic>?>().firstWhere(
            (trip) =>
                trip != null &&
                '${trip['driverId']}'.toLowerCase() ==
                    driverId?.toLowerCase() &&
                ['Accepted', 'InProgress'].contains(
                  _tripStatusLabel(trip['status']),
                ),
            orElse: () => null,
          );
    });
  }

  Future<void> _loadVehicles() async {
    if (driverId == null) {
      vehicle = null;
      return;
    }

    final data = await widget.session.api.get('/api/vehicles', {
      'driverId': driverId,
      'pageSize': 20,
    });
    final items = data is Map ? data['items'] ?? data['Items'] : null;
    final list = items is List ? items : <dynamic>[];
    final vehicles = list
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .toList();

    vehicle = vehicles.isEmpty ? null : vehicles.first;

    if (vehicle != null) {
      _fillVehicleForm(vehicle!);
    }
  }

  Future<void> _connectRealtime() async {
    final currentDriverId = driverId;

    if (currentDriverId == null || widget.session.api.accessToken == null) {
      return;
    }

    await hubConnection?.stop();

    final hubUrl =
        '${widget.session.api.baseUrl.replaceAll(RegExp(r'/+$'), '')}/driverHub';
    final options = HttpConnectionOptions(
      accessTokenFactory: () async => widget.session.api.accessToken ?? '',
    );
    final connection = HubConnectionBuilder()
        .withUrl(hubUrl, options: options)
        .withAutomaticReconnect()
        .build();

    connection.onclose(({error}) {
      if (mounted) {
        setState(() => liveStatus = 'SignalR desconectado.');
      }
    });

    connection.on('DispatchOfferReceived', (args) {
      final offer = _firstMap(args);

      if (offer == null || !mounted) {
        return;
      }

      setState(() {
        currentOffer = offer;
        lastEvent = _pretty('DispatchOfferReceived', offer);
      });
    });

    connection.on('DispatchOfferExpired', (args) {
      final tripId = args?.isNotEmpty == true ? '${args!.first}' : '';

      if (mounted && currentOffer?['tripId']?.toString() == tripId) {
        setState(() {
          currentOffer = null;
          lastEvent = 'DispatchOfferExpired\n$tripId';
        });
      }
    });

    for (final eventName in [
      'TripRequested',
      'TripAccepted',
      'TripStarted',
      'TripFinished',
    ]) {
      connection.on(eventName, (args) {
        final trip = _firstMap(args);

        if (trip == null || !mounted) {
          return;
        }

        _applyTripEvent(eventName, trip);
      });
    }

    connection.on('DriverLocationUpdated', (args) {
      final location = _firstMap(args);

      if (location != null && mounted) {
        setState(() => lastEvent = _pretty('DriverLocationUpdated', location));
      }
    });

    await connection.start();
    await connection.invoke('JoinDriverGroup', args: [currentDriverId]);

    if (!mounted) {
      return;
    }

    setState(() {
      hubConnection = connection;
      liveStatus = 'SignalR conectado.';
    });
  }

  void _applyTripEvent(String eventName, Map<String, dynamic> trip) {
    final tripDriverId = '${trip['driverId'] ?? trip['DriverId']}'.toLowerCase();
    final status = _tripStatusLabel(trip['status'] ?? trip['Status']);

    setState(() {
      lastEvent = _pretty(eventName, trip);

      if (status == 'Requested') {
        final exists = availableTrips.any((item) => _id(item) == _id(trip));
        if (!exists) {
          availableTrips = [trip, ...availableTrips];
        }
      }

      if (tripDriverId == driverId?.toLowerCase()) {
        activeTrip = trip;
        currentOffer = null;
      }

      if (status == 'Finished' && _id(activeTrip ?? {}) == _id(trip)) {
        activeTrip = null;
      }
    });
  }

  Future<void> _updateStatus() async {
    if (driverId == null) {
      return;
    }

    try {
      final data = await widget.session.api.patch(
        '/api/drivers/$driverId/status',
        {'status': selectedStatus},
      ) as Map<String, dynamic>;
      setState(() {
        driver = data;
        lastEvent = 'Status atualizado para ${_driverStatusLabel(data['status'])}.';
      });
      await _loadTrips();
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _saveDriver() async {
    final userId = widget.session.userId;

    if (userId == null) {
      setState(() => lastEvent = 'Faca login novamente para cadastrar motorista.');
      return;
    }

    final body = {
      'userId': userId,
      'cpf': cpf.text.trim(),
      'rg': rg.text.trim(),
      'birthDate': _dateIso(birthDate),
      'phone': phone.text.trim(),
      'emergencyPhone': emergencyPhone.text.trim(),
      'address': address.text.trim(),
      'city': city.text.trim(),
      'state': state.text.trim(),
      'zipCode': zipCode.text.trim(),
      'cnhNumber': cnhNumber.text.trim(),
      'cnhCategory': cnhCategory.text.trim(),
      'cnhExpiration': _dateIso(cnhExpiration),
      'active': _boolValue(driver, 'active', fallback: true),
    };

    try {
      final creating = driver == null;
      final data = driver == null
          ? await widget.session.api.post('/api/drivers', body)
          : await widget.session.api.put('/api/drivers/$driverId', body);

      setState(() {
        driver = Map<String, dynamic>.from(data as Map);
        _fillDriverForm(driver!);
        lastEvent = creating
            ? 'Cadastro de motorista criado.'
            : 'Cadastro de motorista atualizado.';
      });
      await _load();
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _toggleDriverActive() async {
    if (driver == null) {
      return;
    }

    final nextActive = !_boolValue(driver, 'active', fallback: true);
    driver = {...driver!, 'active': nextActive};
    await _saveDriver();
  }

  Future<void> _saveVehicle() async {
    final currentDriverId = driverId;

    if (currentDriverId == null) {
      setState(() => lastEvent = 'Cadastre o motorista antes do veiculo.');
      return;
    }

    final body = {
      'driverId': currentDriverId,
      'plate': vehiclePlate.text.trim(),
      'brand': vehicleBrand.text.trim(),
      'model': vehicleModel.text.trim(),
      'color': vehicleColor.text.trim(),
      'year': int.tryParse(vehicleYear.text.trim()) ?? 0,
      'renavam': vehicleRenavam.text.trim(),
      'chassis': vehicleChassis.text.trim(),
      'active': _boolValue(vehicle, 'active', fallback: true),
    };

    try {
      final data = vehicle == null
          ? await widget.session.api.post('/api/vehicles', body)
          : await widget.session.api.put('/api/vehicles/${_id(vehicle!)}', body);

      setState(() {
        vehicle = Map<String, dynamic>.from(data as Map);
        _fillVehicleForm(vehicle!);
        lastEvent = 'Veiculo salvo.';
      });
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _toggleVehicleActive() async {
    if (vehicle == null) {
      return;
    }

    final nextActive = !_boolValue(vehicle, 'active', fallback: true);
    vehicle = {...vehicle!, 'active': nextActive};
    await _saveVehicle();
  }

  Future<void> _acceptTrip(String tripId) async {
    if (driverId == null || tripId.isEmpty) {
      return;
    }

    try {
      final data = await widget.session.api.post(
        '/api/dispatch/$tripId/accept',
        {'driverId': driverId},
      );
      setState(() {
        activeTrip = Map<String, dynamic>.from(data as Map);
        currentOffer = null;
        lastEvent = _pretty('TripAccepted', activeTrip!);
      });
      await _loadTrips();
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _rejectTrip(String tripId) async {
    if (driverId == null || tripId.isEmpty) {
      return;
    }

    try {
      final data = await widget.session.api.post(
        '/api/dispatch/$tripId/reject',
        {'driverId': driverId, 'reason': 'Recusado pelo app motorista.'},
      );
      setState(() {
        currentOffer = null;
        lastEvent = _pretty('DispatchRejected', data as Map<String, dynamic>);
      });
      await _loadTrips();
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _startTrip(String tripId) async {
    if (driverId == null || tripId.isEmpty) {
      return;
    }

    try {
      final data = await widget.session.api.post(
        '/api/trips/$tripId/start',
        {'driverId': driverId},
      );
      setState(() {
        activeTrip = Map<String, dynamic>.from(data as Map);
        lastEvent = _pretty('TripStarted', activeTrip!);
      });
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _finishTrip(String tripId) async {
    if (driverId == null || tripId.isEmpty) {
      return;
    }

    try {
      final data = await widget.session.api.post(
        '/api/trips/$tripId/finish',
        {
          'driverId': driverId,
          'actualDistanceKm': _doubleValue(actualDistance),
          'actualDurationMinutes': _doubleValue(actualDuration),
        },
      );
      setState(() {
        activeTrip = null;
        lastEvent = _pretty('TripFinished', data as Map<String, dynamic>);
      });
      await _loadTrips();
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _sendLocation() async {
    final currentDriverId = driverId;

    if (currentDriverId == null) {
      return;
    }

    final lat = _doubleValue(latitude);
    final lng = _doubleValue(longitude);
    final currentSpeed = _doubleValue(speed);
    final currentHeading = _doubleValue(heading);

    try {
      if (hubConnection?.state == HubConnectionState.Connected) {
        await hubConnection!.invoke(
          'UpdateLocation',
          args: [currentDriverId, lat, lng, currentSpeed, currentHeading],
        );
      } else {
        await widget.session.api.post(
          '/api/driver-location',
          {},
          {
            'driverId': currentDriverId,
            'latitude': lat,
            'longitude': lng,
            'speed': currentSpeed,
            'heading': currentHeading,
          },
        );
      }

      setState(() {
        lastEvent =
            'DriverLocationUpdated\nlat=$lat lng=$lng speed=$currentSpeed heading=$currentHeading';
      });
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  void _toggleLocationStream() {
    if (locationStreaming) {
      locationTimer?.cancel();
      setState(() {
        locationStreaming = false;
        lastEvent = 'Envio automatico de localizacao parado.';
      });
      return;
    }

    locationTimer = Timer.periodic(
      const Duration(seconds: 5),
      (_) => _sendLocation(),
    );
    _sendLocation();
    setState(() {
      locationStreaming = true;
      lastEvent = 'Envio automatico de localizacao iniciado.';
    });
  }

  Future<void> _logout() async {
    locationTimer?.cancel();
    await hubConnection?.stop();
    await widget.session.logout();

    if (!mounted) {
      return;
    }

    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => LoginPage(
          session: widget.session,
          requiredRole: 'Driver',
        ),
      ),
    );
  }

  static Map<String, dynamic>? _firstMap(List<Object?>? args) {
    if (args == null || args.isEmpty || args.first is! Map) {
      return null;
    }

    return Map<String, dynamic>.from(args.first! as Map);
  }

  static String _id(Map<String, dynamic> source) {
    return '${source['id'] ?? source['Id'] ?? ''}';
  }

  void _fillDriverForm(Map<String, dynamic> source) {
    cpf.text = _value(source, 'cpf');
    rg.text = _value(source, 'rg');
    birthDate.text = _dateValue(source, 'birthDate');
    phone.text = _value(source, 'phone');
    emergencyPhone.text = _value(source, 'emergencyPhone');
    address.text = _value(source, 'address');
    city.text = _value(source, 'city');
    state.text = _value(source, 'state');
    zipCode.text = _value(source, 'zipCode');
    cnhNumber.text = _value(source, 'cnh', fallbackKey: 'cnhNumber');
    cnhCategory.text = _value(source, 'cnhCategory');
    cnhExpiration.text = _dateValue(source, 'cnhExpiration');
  }

  void _fillVehicleForm(Map<String, dynamic> source) {
    vehiclePlate.text = _value(source, 'plate');
    vehicleBrand.text = _value(source, 'brand');
    vehicleModel.text = _value(source, 'model');
    vehicleColor.text = _value(source, 'color');
    vehicleYear.text = _value(source, 'year');
    vehicleRenavam.text = _value(source, 'renavam');
    vehicleChassis.text = _value(source, 'chassis');
  }

  static String _value(
    Map<String, dynamic> source,
    String key, {
    String? fallbackKey,
  }) {
    final pascal = key.substring(0, 1).toUpperCase() + key.substring(1);
    final fallbackPascal = fallbackKey == null
        ? null
        : fallbackKey.substring(0, 1).toUpperCase() + fallbackKey.substring(1);
    final value = source[key] ??
        source[pascal] ??
        (fallbackKey == null ? null : source[fallbackKey]) ??
        (fallbackPascal == null ? null : source[fallbackPascal]);

    return value == null ? '' : '$value';
  }

  static bool _boolValue(
    Map<String, dynamic>? source,
    String key, {
    required bool fallback,
  }) {
    if (source == null) {
      return fallback;
    }

    final value = _value(source, key);

    if (value.isEmpty) {
      return fallback;
    }

    return value.toLowerCase() == 'true';
  }

  static String _dateValue(Map<String, dynamic> source, String key) {
    final value = _value(source, key);

    if (value.length >= 10) {
      return value.substring(0, 10);
    }

    return value;
  }

  static String _dateIso(TextEditingController controller) {
    final value = controller.text.trim();

    if (value.isEmpty) {
      return DateTime.utc(1990).toIso8601String();
    }

    return DateTime.parse(value).toUtc().toIso8601String();
  }

  static double _doubleValue(TextEditingController controller) {
    return double.tryParse(controller.text.trim().replaceAll(',', '.')) ?? 0;
  }

  static String _tripStatusLabel(Object? value) {
    return switch ('$value') {
      '0' => 'Requested',
      '1' => 'Accepted',
      '2' => 'InProgress',
      '3' => 'Finished',
      'Requested' => 'Requested',
      'Accepted' => 'Accepted',
      'InProgress' => 'InProgress',
      'Finished' => 'Finished',
      _ => '$value',
    };
  }

  static String _tripStatusText(Object? value) {
    return switch (_tripStatusLabel(value)) {
      'Requested' => 'Solicitada',
      'Accepted' => 'Aceita',
      'InProgress' => 'Em andamento',
      'Finished' => 'Finalizada',
      _ => '$value',
    };
  }

  static int _driverStatusNumber(Object? value) {
    return switch ('$value') {
      '1' || 'Offline' => 1,
      '2' || 'Online' => 2,
      '3' || 'Busy' => 3,
      '4' || 'Paused' => 4,
      _ => 2,
    };
  }

  static String _driverStatusLabel(Object? value) {
    return switch ('$value') {
      '1' || 'Offline' => 'Offline',
      '2' || 'Online' => 'Online',
      '3' || 'Busy' => 'Ocupado',
      '4' || 'Paused' => 'Pausado',
      _ => '$value',
    };
  }

  static String _pretty(String eventName, Map<String, dynamic> data) {
    const encoder = JsonEncoder.withIndent('  ');
    return '$eventName\n${encoder.convert(data)}';
  }
}

class _DriverRegistrationCard extends StatelessWidget {
  const _DriverRegistrationCard({
    required this.hasDriver,
    required this.cpf,
    required this.rg,
    required this.birthDate,
    required this.phone,
    required this.emergencyPhone,
    required this.address,
    required this.city,
    required this.state,
    required this.zipCode,
    required this.cnhNumber,
    required this.cnhCategory,
    required this.cnhExpiration,
    required this.active,
    required this.onSave,
    required this.onToggleActive,
  });

  final bool hasDriver;
  final TextEditingController cpf;
  final TextEditingController rg;
  final TextEditingController birthDate;
  final TextEditingController phone;
  final TextEditingController emergencyPhone;
  final TextEditingController address;
  final TextEditingController city;
  final TextEditingController state;
  final TextEditingController zipCode;
  final TextEditingController cnhNumber;
  final TextEditingController cnhCategory;
  final TextEditingController cnhExpiration;
  final bool active;
  final VoidCallback onSave;
  final VoidCallback? onToggleActive;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: hasDriver ? 'Cadastro do motorista' : 'Cadastrar motorista',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Expanded(child: _Input(controller: cpf, label: 'CPF')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: rg, label: 'RG')),
            ],
          ),
          Row(
            children: [
              Expanded(
                child: _Input(
                  controller: birthDate,
                  label: 'Nascimento (AAAA-MM-DD)',
                ),
              ),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: phone, label: 'Telefone')),
            ],
          ),
          _Input(controller: emergencyPhone, label: 'Telefone emergencia'),
          _Input(controller: address, label: 'Endereco'),
          Row(
            children: [
              Expanded(child: _Input(controller: city, label: 'Cidade')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: state, label: 'UF')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: zipCode, label: 'CEP')),
            ],
          ),
          Row(
            children: [
              Expanded(child: _Input(controller: cnhNumber, label: 'CNH')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: cnhCategory, label: 'Categoria')),
            ],
          ),
          _Input(
            controller: cnhExpiration,
            label: 'Validade CNH (AAAA-MM-DD)',
          ),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: onSave,
                  icon: const Icon(Icons.save),
                  label: Text(hasDriver ? 'Salvar motorista' : 'Cadastrar motorista'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onToggleActive,
                  icon: Icon(active ? Icons.block : Icons.check_circle),
                  label: Text(active ? 'Desativar' : 'Ativar'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _VehicleRegistrationCard extends StatelessWidget {
  const _VehicleRegistrationCard({
    required this.hasDriver,
    required this.hasVehicle,
    required this.plate,
    required this.brand,
    required this.model,
    required this.color,
    required this.year,
    required this.renavam,
    required this.chassis,
    required this.active,
    required this.onSave,
    required this.onToggleActive,
  });

  final bool hasDriver;
  final bool hasVehicle;
  final TextEditingController plate;
  final TextEditingController brand;
  final TextEditingController model;
  final TextEditingController color;
  final TextEditingController year;
  final TextEditingController renavam;
  final TextEditingController chassis;
  final bool active;
  final VoidCallback? onSave;
  final VoidCallback? onToggleActive;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: hasVehicle ? 'Veiculo cadastrado' : 'Cadastrar veiculo',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          if (!hasDriver)
            const Padding(
              padding: EdgeInsets.only(bottom: 12),
              child: Text('Cadastre o motorista antes do veiculo.'),
            ),
          Row(
            children: [
              Expanded(child: _Input(controller: plate, label: 'Placa')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: year, label: 'Ano')),
            ],
          ),
          Row(
            children: [
              Expanded(child: _Input(controller: brand, label: 'Marca')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: model, label: 'Modelo')),
            ],
          ),
          _Input(controller: color, label: 'Cor'),
          _Input(controller: renavam, label: 'Renavam'),
          _Input(controller: chassis, label: 'Chassi'),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: onSave,
                  icon: const Icon(Icons.save),
                  label: Text(hasVehicle ? 'Salvar veiculo' : 'Cadastrar veiculo'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onToggleActive,
                  icon: Icon(active ? Icons.block : Icons.check_circle),
                  label: Text(active ? 'Desativar' : 'Ativar'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _AvailabilityCard extends StatelessWidget {
  const _AvailabilityCard({
    required this.selectedStatus,
    required this.onChanged,
    required this.onUpdate,
  });

  final int selectedStatus;
  final ValueChanged<int?> onChanged;
  final VoidCallback onUpdate;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Disponibilidade',
      child: Column(
        children: [
          DropdownButtonFormField<int>(
            initialValue: selectedStatus,
            decoration: const InputDecoration(labelText: 'Status'),
            items: const [
              DropdownMenuItem(value: 1, child: Text('Offline')),
              DropdownMenuItem(value: 2, child: Text('Online')),
              DropdownMenuItem(value: 3, child: Text('Ocupado')),
              DropdownMenuItem(value: 4, child: Text('Pausado')),
            ],
            onChanged: onChanged,
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: onUpdate,
              icon: const Icon(Icons.power_settings_new),
              label: const Text('Atualizar status'),
            ),
          ),
        ],
      ),
    );
  }
}

class _OfferCard extends StatelessWidget {
  const _OfferCard({
    required this.offer,
    required this.onAccept,
    required this.onReject,
  });

  final Map<String, dynamic>? offer;
  final VoidCallback? onAccept;
  final VoidCallback? onReject;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Oferta em tempo real',
      child: offer == null
          ? const Text('Nenhuma oferta recebida.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('Origem: ${offer!['origin'] ?? offer!['Origin']}'),
                Text('Destino: ${offer!['destination'] ?? offer!['Destination']}'),
                Text('Distancia ate origem: ${offer!['distanceKm']} km'),
                Text('ETA: ${offer!['etaMinutes']} min'),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: onAccept,
                        icon: const Icon(Icons.check),
                        label: const Text('Aceitar'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: onReject,
                        icon: const Icon(Icons.close),
                        label: const Text('Recusar'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
    );
  }
}

class _ActiveTripCard extends StatelessWidget {
  const _ActiveTripCard({
    required this.trip,
    required this.actualDistance,
    required this.actualDuration,
    required this.canStart,
    required this.canFinish,
    required this.onStart,
    required this.onFinish,
  });

  final Map<String, dynamic>? trip;
  final TextEditingController actualDistance;
  final TextEditingController actualDuration;
  final bool canStart;
  final bool canFinish;
  final VoidCallback? onStart;
  final VoidCallback? onFinish;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Corrida atual',
      child: trip == null
          ? const Text('Nenhuma corrida aceita ou em andamento.')
          : Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text('ID: ${trip!['id'] ?? trip!['Id']}'),
                Text('Origem: ${trip!['origin'] ?? trip!['Origin']}'),
                Text('Destino: ${trip!['destination'] ?? trip!['Destination']}'),
                Text(
                  'Status: ${_DriverHomePageState._tripStatusText(trip!['status'] ?? trip!['Status'])}',
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: _Input(
                        controller: actualDistance,
                        label: 'Distancia km',
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _Input(
                        controller: actualDuration,
                        label: 'Duracao min',
                      ),
                    ),
                  ],
                ),
                Row(
                  children: [
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: canStart ? onStart : null,
                        icon: const Icon(Icons.play_arrow),
                        label: const Text('Iniciar'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: canFinish ? onFinish : null,
                        icon: const Icon(Icons.flag),
                        label: const Text('Finalizar'),
                      ),
                    ),
                  ],
                ),
              ],
            ),
    );
  }
}

class _AvailableTripsCard extends StatelessWidget {
  const _AvailableTripsCard({
    required this.trips,
    required this.onAccept,
  });

  final List<Map<String, dynamic>> trips;
  final ValueChanged<Map<String, dynamic>> onAccept;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Corridas disponiveis',
      child: trips.isEmpty
          ? const Text('Nenhuma corrida aguardando motorista.')
          : Column(
              children: trips
                  .map(
                    (trip) => ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text('${trip['origin'] ?? trip['Origin']}'),
                      subtitle: Text('${trip['destination'] ?? trip['Destination']}'),
                      trailing: FilledButton(
                        onPressed: () => onAccept(trip),
                        child: const Text('Aceitar'),
                      ),
                    ),
                  )
                  .toList(),
            ),
    );
  }
}

class _LocationCard extends StatelessWidget {
  const _LocationCard({
    required this.latitude,
    required this.longitude,
    required this.speed,
    required this.heading,
    required this.streaming,
    required this.onSend,
    required this.onToggleStream,
  });

  final TextEditingController latitude;
  final TextEditingController longitude;
  final TextEditingController speed;
  final TextEditingController heading;
  final bool streaming;
  final VoidCallback onSend;
  final VoidCallback onToggleStream;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Localizacao em tempo real',
      child: Column(
        children: [
          Row(
            children: [
              Expanded(child: _Input(controller: latitude, label: 'Latitude')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: longitude, label: 'Longitude')),
            ],
          ),
          Row(
            children: [
              Expanded(child: _Input(controller: speed, label: 'Velocidade')),
              const SizedBox(width: 12),
              Expanded(child: _Input(controller: heading, label: 'Direcao')),
            ],
          ),
          Row(
            children: [
              Expanded(
                child: FilledButton.icon(
                  onPressed: onSend,
                  icon: const Icon(Icons.my_location),
                  label: const Text('Enviar agora'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onToggleStream,
                  icon: Icon(streaming ? Icons.pause : Icons.play_arrow),
                  label: Text(streaming ? 'Parar envio' : 'Enviar a cada 5s'),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _HeaderCard extends StatelessWidget {
  const _HeaderCard({
    required this.title,
    required this.subtitle,
    required this.trailing,
  });

  final String title;
  final String subtitle;
  final String trailing;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            const CircleAvatar(child: Icon(Icons.badge)),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title, style: Theme.of(context).textTheme.titleLarge),
                  Text(subtitle),
                ],
              ),
            ),
            Flexible(child: Text(trailing, textAlign: TextAlign.end)),
          ],
        ),
      ),
    );
  }
}

class _SectionCard extends StatelessWidget {
  const _SectionCard({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            child,
          ],
        ),
      ),
    );
  }
}

class _DebugCard extends StatelessWidget {
  const _DebugCard({required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return _SectionCard(
      title: 'Debug realtime',
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: const Color(0xff101828),
          borderRadius: BorderRadius.circular(8),
        ),
        child: SizedBox(
          width: double.infinity,
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(12),
            scrollDirection: Axis.horizontal,
            child: SelectableText(
              text,
              style: const TextStyle(
                color: Colors.white,
                fontFamily: 'monospace',
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Input extends StatelessWidget {
  const _Input({
    required this.controller,
    required this.label,
    this.obscureText = false,
  });

  final TextEditingController controller;
  final String label;
  final bool obscureText;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextField(
        controller: controller,
        obscureText: obscureText,
        decoration: InputDecoration(
          border: const OutlineInputBorder(),
          labelText: label,
        ),
      ),
    );
  }
}
