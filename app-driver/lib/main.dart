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

  HubConnection? hubConnection;
  Timer? locationTimer;
  Map<String, dynamic>? driver;
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
      driver = await widget.session.api.get('/api/drivers/by-user/$userId')
          as Map<String, dynamic>;
      selectedStatus = _driverStatusNumber(driver!['status']);
      await _connectRealtime();
      await _loadTrips();
    } catch (ex) {
      lastEvent = ex.toString();
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _loadTrips() async {
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
      '1' => 'Offline',
      '2' => 'Online',
      '3' => 'Busy',
      '4' => 'Paused',
      _ => '$value',
    };
  }

  static String _pretty(String eventName, Map<String, dynamic> data) {
    const encoder = JsonEncoder.withIndent('  ');
    return '$eventName\n${encoder.convert(data)}';
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
    required this.onStart,
    required this.onFinish,
  });

  final Map<String, dynamic>? trip;
  final TextEditingController actualDistance;
  final TextEditingController actualDuration;
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
                Text('Status: ${trip!['status'] ?? trip!['Status']}'),
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
                        onPressed: onStart,
                        icon: const Icon(Icons.play_arrow),
                        label: const Text('Iniciar'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: onFinish,
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
