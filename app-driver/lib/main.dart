import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart' hide ConnectionState;

const defaultApiBaseUrl = 'http://45.185.199.173:8282';

void main() {
  runApp(const RidePrDriverApp());
}

class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'RIDEPR_API_URL',
    defaultValue: defaultApiBaseUrl,
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

  Future<void> register(
    String baseUrl,
    String name,
    String email,
    String password,
  ) async {
    api.baseUrl = baseUrl.trim().replaceAll(RegExp(r'/+$'), '');
    final data = await api.post('/api/auth/register', {
      'name': name.trim().isEmpty ? 'Motorista RidePR' : name.trim(),
      'email': email.trim(),
      'password': password,
      'role': 2,
    }) as Map<String, dynamic>;

    api.accessToken = data['accessToken'] as String?;
    refreshToken = data['refreshToken'] as String?;
    userId = data['userId'] as String?;
    this.name = data['name'] as String?;
    this.email = data['email'] as String?;
    role = data['role'] as String?;

    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('baseUrl', api.baseUrl);
    await prefs.setString('accessToken', api.accessToken ?? '');
    await prefs.setString('refreshToken', refreshToken ?? '');
    await prefs.setString('userId', userId ?? '');
    await prefs.setString('name', this.name ?? '');
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
  final name = TextEditingController();
  final email = TextEditingController();
  final password = TextEditingController();
  final confirmPassword = TextEditingController();
  bool loading = false;
  String mode = 'entry';
  String? error;

  @override
  void dispose() {
    baseUrl.dispose();
    name.dispose();
    email.dispose();
    password.dispose();
    confirmPassword.dispose();
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
                  mode == 'register'
                      ? 'Crie sua conta de motorista'
                      : mode == 'login'
                          ? 'Entre para receber corridas'
                          : 'Dirija com um app simples e profissional',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 28),
                if (mode != 'entry')
                  _Input(controller: baseUrl, label: 'Endereco da API'),
                if (mode == 'register') _Input(controller: name, label: 'Nome'),
                if (mode != 'entry') _Input(controller: email, label: 'E-mail'),
                if (mode != 'entry')
                  _Input(
                    controller: password,
                    label: 'Senha',
                    obscureText: true,
                  ),
                if (mode == 'register')
                  _Input(
                    controller: confirmPassword,
                    label: 'Confirmar senha',
                    obscureText: true,
                  ),
                if (error != null) ...[
                  const SizedBox(height: 8),
                  Text(error!, style: const TextStyle(color: Colors.red)),
                ],
                const SizedBox(height: 12),
                if (mode == 'entry') ...[
                  FilledButton.icon(
                    onPressed: () => setState(() => mode = 'login'),
                    icon: const Icon(Icons.login),
                    label: const Text('Entrar'),
                  ),
                  OutlinedButton.icon(
                    onPressed: () => setState(() => mode = 'register'),
                    icon: const Icon(Icons.person_add),
                    label: const Text('Criar conta'),
                  ),
                ] else ...[
                  FilledButton.icon(
                    onPressed: loading
                        ? null
                        : mode == 'register'
                            ? _register
                            : _submit,
                    icon: Icon(
                        mode == 'register' ? Icons.person_add : Icons.login),
                    label: Text(
                      loading
                          ? 'Aguarde...'
                          : mode == 'register'
                              ? 'Criar conta'
                              : 'Entrar',
                    ),
                  ),
                  TextButton(
                    onPressed:
                        loading ? null : () => setState(() => mode = 'entry'),
                    child: const Text('Voltar'),
                  ),
                ],
                TextButton(
                  onPressed: () => ScaffoldMessenger.of(context).showSnackBar(
                    const SnackBar(
                      content: Text(
                          'Recuperacao de senha ainda nao esta disponivel.'),
                    ),
                  ),
                  child: const Text('Recuperar senha'),
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

  Future<void> _register() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      if (password.text != confirmPassword.text) {
        throw ApiException('As senhas nao conferem.');
      }

      await widget.session.register(
        baseUrl.text.trim(),
        name.text.trim(),
        email.text.trim(),
        password.text,
      );

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
  final latitude = TextEditingController();
  final longitude = TextEditingController();
  final speed = TextEditingController(text: '0');
  final heading = TextEditingController(text: '0');
  final actualDistance = TextEditingController(text: '4.2');
  final actualDuration = TextEditingController(text: '18');
  final cpf = TextEditingController();
  final rg = TextEditingController();
  final birthDate = TextEditingController();
  final phone = TextEditingController();
  final emergencyPhone = TextEditingController();
  final address = TextEditingController();
  final city = TextEditingController();
  final state = TextEditingController();
  final zipCode = TextEditingController();
  final cnhNumber = TextEditingController();
  final cnhCategory = TextEditingController();
  final cnhExpiration = TextEditingController();
  final vehiclePlate = TextEditingController();
  final vehicleBrand = TextEditingController();
  final vehicleModel = TextEditingController();
  final vehicleColor = TextEditingController();
  final vehicleYear = TextEditingController();
  final vehicleRenavam = TextEditingController();
  final vehicleChassis = TextEditingController();

  final mapController = MapController();
  HubConnection? hubConnection;
  Timer? locationTimer;
  String? connectedHubUrl;
  Map<String, dynamic>? driver;
  Map<String, dynamic>? vehicle;
  Map<String, dynamic>? currentOffer;
  Map<String, dynamic>? activeTrip;
  List<Map<String, dynamic>> availableTrips = [];
  LatLng? driverPoint;
  LatLng? originPoint;
  LatLng? destinationPoint;
  LatLng? lastCenteredPoint;
  LatLng? lastSentLocationPoint;
  String? lastLocationSignature;
  bool loading = true;
  bool locationStreaming = false;
  int selectedStatus = 2;
  String liveStatus = 'SignalR desconectado.';
  String lastEvent = 'Nenhum evento recebido ainda.';
  String? locationError;
  bool offerDialogOpen = false;

  String? get driverId => driver?['id']?.toString();
  bool get driverReady =>
      driver != null &&
      vehicle != null &&
      _boolValue(driver, 'active', fallback: false) &&
      _boolValue(vehicle, 'active', fallback: false);

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
    if (!loading && !driverReady) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Complete seu cadastro'),
          actions: [
            IconButton(
              onPressed: _logout,
              icon: const Icon(Icons.logout),
              tooltip: 'Sair',
            ),
          ],
        ),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              'Para ficar online, complete o cadastro do motorista, cadastre um veiculo e mantenha ambos ativos.',
              style: Theme.of(context).textTheme.titleMedium,
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
            const SizedBox(height: 12),
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
            if (loading) const LinearProgressIndicator(),
          ],
        ),
      );
    }

    return Scaffold(
      endDrawer: Drawer(
        child: SafeArea(
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(
                'RidePR Motorista',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              const SizedBox(height: 12),
              _HeaderCard(
                title: widget.session.name ?? 'Motorista',
                subtitle: driver == null
                    ? widget.session.email ?? ''
                    : 'Status: ${_driverStatusLabel(driver!['status'])}',
                trailing: driver == null ? 'Sem cadastro' : liveStatus,
              ),
              const SizedBox(height: 12),
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
              const SizedBox(height: 12),
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
              const SizedBox(height: 12),
              ListTile(
                leading: const Icon(Icons.person),
                title: const Text('Perfil'),
                subtitle: const Text('Dados pessoais, CNH e veiculo'),
                onTap: () {
                  Navigator.of(context).pop();
                  _openProfile();
                },
              ),
              ListTile(
                leading: const Icon(Icons.history),
                title: const Text('Minhas corridas'),
                subtitle: const Text('Historico do motorista'),
                onTap: () {
                  Navigator.of(context).pop();
                  _openHistory();
                },
              ),
              const SizedBox(height: 12),
              ExpansionTile(
                leading: const Icon(Icons.settings),
                title: const Text('Suporte'),
                children: [
                  _LocationCard(
                    latitude: latitude,
                    longitude: longitude,
                    speed: speed,
                    heading: heading,
                    streaming: locationStreaming,
                    onSend: _sendLocation,
                    onToggleStream: _toggleLocationStream,
                  ),
                  _DebugCard(text: lastEvent),
                ],
              ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: _logout,
                icon: const Icon(Icons.logout),
                label: const Text('Sair'),
              ),
            ],
          ),
        ),
      ),
      body: Stack(
        children: [
          Positioned.fill(
            child: FlutterMap(
              mapController: mapController,
              options: const MapOptions(
                initialCenter: LatLng(-23.555, -46.645),
                initialZoom: 13,
              ),
              children: [
                TileLayer(
                  urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                  userAgentPackageName: 'com.ridepr.driver',
                ),
                MarkerLayer(markers: _markers()),
              ],
            ),
          ),
          SafeArea(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Builder(
                    builder: (context) => FloatingActionButton.small(
                      heroTag: 'driver-menu',
                      onPressed: () => Scaffold.of(context).openEndDrawer(),
                      child: const Icon(Icons.menu),
                    ),
                  ),
                  const Spacer(),
                  FloatingActionButton.small(
                    heroTag: 'driver-refresh',
                    onPressed: loading ? null : _load,
                    child: const Icon(Icons.refresh),
                  ),
                ],
              ),
            ),
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: _DriverRidePanel(
              driverReady: driver != null && vehicle != null,
              canOperate: driverReady,
              online: selectedStatus == 2,
              streaming: locationStreaming,
              liveStatus: liveStatus,
              locationError: locationError,
              offer: currentOffer,
              activeTrip: activeTrip,
              actualDistance: actualDistance,
              actualDuration: actualDuration,
              onToggleOnline: _toggleAvailability,
              onAccept: currentOffer == null
                  ? null
                  : () => _acceptTrip(currentOffer!['tripId'].toString()),
              onReject: currentOffer == null
                  ? null
                  : () => _rejectTrip(currentOffer!['tripId'].toString()),
              onStart: activeTrip == null
                  ? null
                  : () => _startTrip(_id(activeTrip!)),
              onFinish: activeTrip == null
                  ? null
                  : () => _finishTrip(_id(activeTrip!)),
              canStart: _tripStatusLabel(
                    activeTrip?['status'] ?? activeTrip?['Status'],
                  ) ==
                  'Accepted',
              canFinish: _tripStatusLabel(
                    activeTrip?['status'] ?? activeTrip?['Status'],
                  ) ==
                  'InProgress',
            ),
          ),
          if (loading)
            const Positioned(
              left: 0,
              right: 0,
              top: 0,
              child: LinearProgressIndicator(minHeight: 3),
            ),
        ],
      ),
    );
  }

  List<Marker> _markers() {
    return [
      if (driverPoint != null)
        _mapMarker(driverPoint!, Icons.local_taxi, Colors.blue),
      if (originPoint != null)
        _mapMarker(originPoint!, Icons.trip_origin, Colors.green),
      if (destinationPoint != null)
        _mapMarker(destinationPoint!, Icons.flag, Colors.red),
    ];
  }

  static Marker _mapMarker(LatLng point, IconData icon, Color color) {
    return Marker(
      point: point,
      width: 44,
      height: 44,
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: Colors.white,
          border: Border.all(color: color, width: 2),
          borderRadius: BorderRadius.circular(22),
        ),
        child: Icon(icon, color: color),
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

    final hubUrl =
        '${widget.session.api.baseUrl.replaceAll(RegExp(r'/+$'), '')}/driverHub';

    if (connectedHubUrl == hubUrl &&
        hubConnection?.state == HubConnectionState.Connected) {
      return;
    }

    await hubConnection?.stop();

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
        _applyRoutePoints(offer);
        lastEvent = _pretty('DispatchOfferReceived', offer);
      });
      _presentOffer(offer);
    });

    connection.on('DispatchOfferExpired', (args) {
      final tripId = args?.isNotEmpty == true ? '${args!.first}' : '';

      if (mounted && currentOffer?['tripId']?.toString() == tripId) {
        setState(() {
          currentOffer = null;
          lastEvent = 'DispatchOfferExpired\n$tripId';
        });
        _closeOfferDialog();
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
      connectedHubUrl = hubUrl;
      liveStatus = 'SignalR conectado.';
    });
  }

  void _applyTripEvent(String eventName, Map<String, dynamic> trip) {
    final tripDriverId =
        '${trip['driverId'] ?? trip['DriverId']}'.toLowerCase();
    final status = _tripStatusLabel(trip['status'] ?? trip['Status']);

    setState(() {
      lastEvent = _pretty(eventName, trip);
      _applyRoutePoints(trip);

      if (status == 'Requested') {
        final exists = availableTrips.any((item) => _id(item) == _id(trip));
        if (!exists) {
          availableTrips = [trip, ...availableTrips];
        }
      } else {
        availableTrips =
            availableTrips.where((item) => _id(item) != _id(trip)).toList();
      }

      if (tripDriverId == driverId?.toLowerCase()) {
        activeTrip = trip;
        currentOffer = null;
      }

      if (status == 'Finished' && _id(activeTrip ?? {}) == _id(trip)) {
        activeTrip = null;
      }
    });
    _moveMapToVisiblePoint();
  }

  void _presentOffer(Map<String, dynamic> offer) {
    if (offerDialogOpen || !mounted) {
      return;
    }

    offerDialogOpen = true;
    SystemSound.play(SystemSoundType.alert);
    HapticFeedback.mediumImpact();

    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (_) => _OfferDialog(
        offer: offer,
        seconds: 20,
        onAccept: () {
          Navigator.of(context, rootNavigator: true).pop();
          offerDialogOpen = false;
          _acceptTrip('${offer['tripId'] ?? offer['TripId']}');
        },
        onReject: () {
          Navigator.of(context, rootNavigator: true).pop();
          offerDialogOpen = false;
          _rejectTrip('${offer['tripId'] ?? offer['TripId']}');
        },
        onTimeout: () {
          if (!mounted || !offerDialogOpen) {
            return;
          }

          Navigator.of(context, rootNavigator: true).pop();
          offerDialogOpen = false;
          _rejectTrip('${offer['tripId'] ?? offer['TripId']}');
        },
      ),
    ).whenComplete(() => offerDialogOpen = false);
  }

  void _closeOfferDialog() {
    if (!offerDialogOpen || !mounted) {
      return;
    }

    Navigator.of(context, rootNavigator: true).maybePop();
    offerDialogOpen = false;
  }

  Future<void> _updateStatus() async {
    if (driverId == null) {
      return;
    }

    if (!driverReady && selectedStatus == 2) {
      setState(() {
        selectedStatus = 1;
        lastEvent = 'Ative o cadastro e o veiculo antes de ficar online.';
      });
      return;
    }

    try {
      final data = await widget.session.api.patch(
        '/api/drivers/$driverId/status',
        {'status': selectedStatus},
      ) as Map<String, dynamic>;
      setState(() {
        driver = data;
        lastEvent =
            'Status atualizado para ${_driverStatusLabel(data['status'])}.';
      });
      if (selectedStatus == 2 && !locationStreaming) {
        _startLocationStream();
      }
      if (selectedStatus != 2 && locationStreaming) {
        _stopLocationStream('Envio automatico de localizacao parado.');
      }
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _toggleAvailability() async {
    setState(() => selectedStatus = selectedStatus == 2 ? 1 : 2);
    await _updateStatus();
  }

  Future<void> _saveDriver() async {
    final userId = widget.session.userId;

    if (userId == null) {
      setState(
          () => lastEvent = 'Faca login novamente para cadastrar motorista.');
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
          : await widget.session.api
              .put('/api/vehicles/${_id(vehicle!)}', body);

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
        _applyRoutePoints(activeTrip!);
        currentOffer = null;
        availableTrips =
            availableTrips.where((trip) => _id(trip) != tripId).toList();
        lastEvent = _pretty('TripAccepted', activeTrip!);
      });
      _moveMapToVisiblePoint();
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
        availableTrips =
            availableTrips.where((trip) => _id(trip) != tripId).toList();
        lastEvent = _pretty('DispatchRejected', data as Map<String, dynamic>);
      });
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
        _applyRoutePoints(activeTrip!);
        lastEvent = _pretty('TripStarted', activeTrip!);
      });
      _moveMapToVisiblePoint();
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
        originPoint = null;
        destinationPoint = null;
        lastEvent = _pretty('TripFinished', data as Map<String, dynamic>);
      });
    } catch (ex) {
      setState(() => lastEvent = ex.toString());
    }
  }

  Future<void> _sendLocation() async {
    final currentDriverId = driverId;

    if (currentDriverId == null) {
      return;
    }

    if (selectedStatus != 2 && activeTrip == null) {
      return;
    }

    final position = await _readCurrentPosition();

    if (position == null) {
      return;
    }

    final lat = position.latitude;
    final lng = position.longitude;
    final currentSpeed = position.speed.isFinite && position.speed >= 0
        ? position.speed
        : _doubleValue(speed);
    final currentHeading = position.heading.isFinite && position.heading >= 0
        ? position.heading
        : _doubleValue(heading);
    final nextPoint = LatLng(lat, lng);
    final movedMeters = lastSentLocationPoint == null
        ? double.infinity
        : const Distance().as(
            LengthUnit.Meter,
            lastSentLocationPoint!,
            nextPoint,
          );
    final signature =
        '${lat.toStringAsFixed(5)}:${lng.toStringAsFixed(5)}:${currentSpeed.toStringAsFixed(1)}:${currentHeading.toStringAsFixed(0)}';

    if (signature == lastLocationSignature || movedMeters < 8) {
      return;
    }

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
        lastLocationSignature = signature;
        lastSentLocationPoint = nextPoint;
        driverPoint = nextPoint;
        latitude.text = lat.toStringAsFixed(6);
        longitude.text = lng.toStringAsFixed(6);
        speed.text = currentSpeed.toStringAsFixed(1);
        heading.text = currentHeading.toStringAsFixed(0);
        locationError = null;
        lastEvent =
            'DriverLocationUpdated\nlat=$lat lng=$lng speed=$currentSpeed heading=$currentHeading';
      });
      _moveMapToVisiblePoint();
    } catch (ex) {
      setState(() {
        locationError = _friendlyLocationError(ex);
        lastEvent = locationError!;
      });
    }
  }

  Future<Position?> _readCurrentPosition() async {
    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();

      if (!serviceEnabled) {
        setState(() {
          locationError = 'GPS desligado. Ative a localizacao do celular.';
          lastEvent = locationError!;
        });
        return null;
      }

      var permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied) {
        setState(() {
          locationError = 'Permissao de localizacao negada.';
          lastEvent = locationError!;
        });
        return null;
      }

      if (permission == LocationPermission.deniedForever) {
        setState(() {
          locationError =
              'Permissao de localizacao bloqueada. Libere nas configuracoes do Android.';
          lastEvent = locationError!;
        });
        return null;
      }

      return Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } catch (ex) {
      setState(() {
        locationError = _friendlyLocationError(ex);
        lastEvent = locationError!;
      });
      return null;
    }
  }

  static String _friendlyLocationError(Object error) {
    final message = '$error';

    if (message.contains('TimeoutException')) {
      return 'Nao consegui pegar o GPS agora. Tente em local aberto.';
    }

    return message.replaceFirst('Exception: ', '');
  }

  void _toggleLocationStream() {
    if (locationStreaming) {
      _stopLocationStream('Envio automatico de localizacao parado.');
      return;
    }

    _startLocationStream();
  }

  void _startLocationStream() {
    if (locationStreaming) {
      return;
    }

    setState(() {
      locationStreaming = true;
      lastEvent = 'Envio automatico de localizacao iniciado.';
    });
    locationTimer = Timer.periodic(
      const Duration(seconds: 5),
      (_) {
        if (mounted &&
            locationStreaming &&
            (selectedStatus == 2 || activeTrip != null)) {
          _sendLocation();
        }
      },
    );
    _sendLocation();
  }

  void _stopLocationStream(String message) {
    locationTimer?.cancel();
    locationTimer = null;
    setState(() {
      locationStreaming = false;
      lastEvent = message;
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

  void _openProfile() {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => Scaffold(
          appBar: AppBar(title: const Text('Perfil do motorista')),
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: [
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
              const SizedBox(height: 12),
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
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: _logout,
                icon: const Icon(Icons.logout),
                label: const Text('Sair'),
              ),
            ],
          ),
        ),
      ),
    );
  }

  void _openHistory() {
    final currentDriverId = driverId;

    if (currentDriverId == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Complete o cadastro do motorista.')),
      );
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => _DriverHistoryPage(
          api: widget.session.api,
          driverId: currentDriverId,
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

  void _applyRoutePoints(Map<String, dynamic> source) {
    final originLat = _numValue(source, 'originLatitude');
    final originLng = _numValue(source, 'originLongitude');
    final destLat = _numValue(source, 'destinationLatitude');
    final destLng = _numValue(source, 'destinationLongitude');

    if (originLat != 0 && originLng != 0) {
      originPoint = LatLng(originLat, originLng);
    }

    if (destLat != 0 && destLng != 0) {
      destinationPoint = LatLng(destLat, destLng);
    }
  }

  void _moveMapToVisiblePoint() {
    final point = activeTrip == null
        ? (driverPoint ?? originPoint ?? destinationPoint)
        : (originPoint ?? destinationPoint ?? driverPoint);

    if (point != null && !_samePoint(point, lastCenteredPoint)) {
      lastCenteredPoint = point;
      mapController.move(point, 14);
    }
  }

  static bool _samePoint(LatLng? left, LatLng? right) {
    if (left == null || right == null) {
      return false;
    }

    return (left.latitude - right.latitude).abs() < 0.00001 &&
        (left.longitude - right.longitude).abs() < 0.00001;
  }

  static String _id(Map<String, dynamic> source) {
    return '${source['id'] ?? source['Id'] ?? ''}';
  }

  static double _numValue(Map<String, dynamic> source, String key) {
    final value = source[key] ??
        source[key.substring(0, 1).toUpperCase() + key.substring(1)];

    if (value is num) {
      return value.toDouble();
    }

    return double.tryParse('$value') ?? 0;
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
              Expanded(
                  child: _Input(controller: cnhCategory, label: 'Categoria')),
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
                  label: Text(
                      hasDriver ? 'Salvar motorista' : 'Cadastrar motorista'),
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
                  label:
                      Text(hasVehicle ? 'Salvar veiculo' : 'Cadastrar veiculo'),
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

class _DriverHistoryPage extends StatefulWidget {
  const _DriverHistoryPage({
    required this.api,
    required this.driverId,
  });

  final ApiClient api;
  final String driverId;

  @override
  State<_DriverHistoryPage> createState() => _DriverHistoryPageState();
}

class _DriverHistoryPageState extends State<_DriverHistoryPage> {
  late Future<List<Map<String, dynamic>>> trips = _load();

  Future<List<Map<String, dynamic>>> _load() async {
    final data = await widget.api.get('/api/trips');
    final list = data is List ? data : <dynamic>[];

    return list
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .where(
          (trip) =>
              '${trip['driverId'] ?? trip['DriverId']}'.toLowerCase() ==
              widget.driverId.toLowerCase(),
        )
        .toList()
      ..sort(
        (a, b) => '${b['createdAt'] ?? b['CreatedAt']}'
            .compareTo('${a['createdAt'] ?? a['CreatedAt']}'),
      );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Minhas corridas')),
      body: FutureBuilder<List<Map<String, dynamic>>>(
        future: trips,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
                child: Text('Nao foi possivel carregar o historico.'));
          }

          final rows = snapshot.data ?? [];

          if (rows.isEmpty) {
            return const Center(child: Text('Nenhuma corrida encontrada.'));
          }

          return RefreshIndicator(
            onRefresh: () async {
              setState(() => trips = _load());
              await trips;
            },
            child: ListView.separated(
              padding: const EdgeInsets.all(16),
              itemBuilder: (context, index) {
                final trip = rows[index];
                final date = _dateLabel(trip['createdAt'] ?? trip['CreatedAt']);
                final price = trip['price'] ?? trip['Price'];

                return ListTile(
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 12,
                    vertical: 8,
                  ),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                    side: BorderSide(color: Theme.of(context).dividerColor),
                  ),
                  title: Text('${trip['origin'] ?? trip['Origin'] ?? '-'}'),
                  subtitle: Text(
                    '${trip['destination'] ?? trip['Destination'] ?? '-'}\n'
                    '$date • ${_statusLabel(trip['status'] ?? trip['Status'])}',
                  ),
                  isThreeLine: true,
                  trailing: Text(price == null ? '-' : 'R\$ $price'),
                );
              },
              separatorBuilder: (_, __) => const SizedBox(height: 10),
              itemCount: rows.length,
            ),
          );
        },
      ),
    );
  }

  static String _dateLabel(Object? value) {
    if (value == null) {
      return 'Sem data';
    }

    final parsed = DateTime.tryParse('$value')?.toLocal();

    if (parsed == null) {
      return '$value';
    }

    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final date = DateTime(parsed.year, parsed.month, parsed.day);

    if (date == today) {
      return 'Hoje';
    }

    if (date == today.subtract(const Duration(days: 1))) {
      return 'Ontem';
    }

    return '${parsed.day.toString().padLeft(2, '0')}/'
        '${parsed.month.toString().padLeft(2, '0')}/${parsed.year}';
  }

  static String _statusLabel(Object? status) {
    return switch ('$status') {
      '0' || 'Requested' => 'Solicitada',
      '1' || 'Accepted' => 'Aceita',
      '2' || 'InProgress' => 'Em andamento',
      '3' || 'Finished' => 'Finalizada',
      _ => '$status',
    };
  }
}

class _OfferDialog extends StatefulWidget {
  const _OfferDialog({
    required this.offer,
    required this.seconds,
    required this.onAccept,
    required this.onReject,
    required this.onTimeout,
  });

  final Map<String, dynamic> offer;
  final int seconds;
  final VoidCallback onAccept;
  final VoidCallback onReject;
  final VoidCallback onTimeout;

  @override
  State<_OfferDialog> createState() => _OfferDialogState();
}

class _OfferDialogState extends State<_OfferDialog> {
  late int remaining = widget.seconds;
  Timer? timer;

  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (remaining <= 1) {
        timer?.cancel();
        widget.onTimeout();
        return;
      }

      setState(() => remaining--);
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final price = widget.offer['price'] ?? widget.offer['Price'];
    final distance =
        widget.offer['distanceKm'] ?? widget.offer['DistanceKm'] ?? '-';
    final eta = widget.offer['etaMinutes'] ?? widget.offer['EtaMinutes'] ?? '-';

    return Dialog.fullscreen(
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  const Icon(Icons.local_taxi, size: 32),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Nova corrida',
                      style: Theme.of(context).textTheme.headlineMedium,
                    ),
                  ),
                  CircleAvatar(child: Text('$remaining')),
                ],
              ),
              const SizedBox(height: 28),
              _OfferLine(
                icon: Icons.trip_origin,
                label: 'Origem',
                value:
                    '${widget.offer['origin'] ?? widget.offer['Origin'] ?? '-'}',
              ),
              const SizedBox(height: 16),
              _OfferLine(
                icon: Icons.flag,
                label: 'Destino',
                value:
                    '${widget.offer['destination'] ?? widget.offer['Destination'] ?? '-'}',
              ),
              const SizedBox(height: 24),
              Wrap(
                spacing: 10,
                runSpacing: 10,
                children: [
                  _InfoChip(label: 'Distancia', value: '$distance km'),
                  _InfoChip(label: 'Tempo', value: '$eta min'),
                  if (price != null)
                    _InfoChip(label: 'Valor', value: 'R\$ $price'),
                ],
              ),
              const Spacer(),
              FilledButton(
                onPressed: widget.onAccept,
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(56),
                ),
                child: const Text('Aceitar'),
              ),
              const SizedBox(height: 12),
              OutlinedButton(
                onPressed: widget.onReject,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size.fromHeight(56),
                ),
                child: const Text('Recusar'),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _OfferLine extends StatelessWidget {
  const _OfferLine({
    required this.icon,
    required this.label,
    required this.value,
  });

  final IconData icon;
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Icon(icon),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label, style: Theme.of(context).textTheme.labelLarge),
              const SizedBox(height: 4),
              Text(value, style: Theme.of(context).textTheme.titleMedium),
            ],
          ),
        ),
      ],
    );
  }
}

class _InfoChip extends StatelessWidget {
  const _InfoChip({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Chip(
      label: Text('$label: $value'),
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
    );
  }
}

class _DriverRidePanel extends StatelessWidget {
  const _DriverRidePanel({
    required this.driverReady,
    required this.canOperate,
    required this.online,
    required this.streaming,
    required this.liveStatus,
    required this.locationError,
    required this.offer,
    required this.activeTrip,
    required this.actualDistance,
    required this.actualDuration,
    required this.onToggleOnline,
    required this.onAccept,
    required this.onReject,
    required this.onStart,
    required this.onFinish,
    required this.canStart,
    required this.canFinish,
  });

  final bool driverReady;
  final bool canOperate;
  final bool online;
  final bool streaming;
  final String liveStatus;
  final String? locationError;
  final Map<String, dynamic>? offer;
  final Map<String, dynamic>? activeTrip;
  final TextEditingController actualDistance;
  final TextEditingController actualDuration;
  final VoidCallback onToggleOnline;
  final VoidCallback? onAccept;
  final VoidCallback? onReject;
  final VoidCallback? onStart;
  final VoidCallback? onFinish;
  final bool canStart;
  final bool canFinish;

  @override
  Widget build(BuildContext context) {
    final hasOffer = offer != null;
    final hasTrip = activeTrip != null;

    return SafeArea(
      top: false,
      child: Container(
        margin: const EdgeInsets.all(12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(18),
          boxShadow: const [
            BoxShadow(
              color: Colors.black26,
              blurRadius: 22,
              offset: Offset(0, 10),
            ),
          ],
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Icon(
                  online ? Icons.radio_button_checked : Icons.radio_button_off,
                  color: online ? Colors.green : Colors.black54,
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _headline,
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(locationError ??
                (streaming ? 'Localizacao automatica ativa' : liveStatus)),
            const SizedBox(height: 14),
            if (!driverReady)
              const Text('Complete cadastro e veiculo no menu para operar.')
            else if (!canOperate)
              const Text(
                'Ative o cadastro e o veiculo no menu para ficar online.',
              )
            else if (hasOffer)
              _OfferSummary(offer: offer!)
            else if (hasTrip)
              _TripSummary(
                trip: activeTrip!,
                actualDistance: actualDistance,
                actualDuration: actualDuration,
              )
            else
              Text(
                online
                    ? 'Aguardando uma corrida perto de voce.'
                    : 'Fique online para receber corridas.',
              ),
            const SizedBox(height: 14),
            if (hasOffer)
              Row(
                children: [
                  Expanded(
                    child: FilledButton(
                      onPressed: onAccept,
                      style: FilledButton.styleFrom(
                        minimumSize: const Size.fromHeight(52),
                      ),
                      child: const Text('Aceitar'),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: onReject,
                      style: OutlinedButton.styleFrom(
                        minimumSize: const Size.fromHeight(52),
                      ),
                      child: const Text('Recusar'),
                    ),
                  ),
                ],
              )
            else if (hasTrip)
              FilledButton(
                onPressed: canFinish
                    ? onFinish
                    : canStart
                        ? onStart
                        : null,
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(52),
                ),
                child:
                    Text(canFinish ? 'Finalizar corrida' : 'Iniciar corrida'),
              )
            else
              FilledButton(
                onPressed: canOperate ? onToggleOnline : null,
                style: FilledButton.styleFrom(
                  minimumSize: const Size.fromHeight(54),
                  textStyle: const TextStyle(
                    fontSize: 17,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                child: Text(online ? 'Ficar offline' : 'Ficar online'),
              ),
          ],
        ),
      ),
    );
  }

  String get _headline {
    if (!driverReady) {
      return 'Prepare seu perfil';
    }

    if (!canOperate) {
      return 'Ative seu cadastro';
    }

    if (offer != null) {
      return 'Nova corrida';
    }

    if (activeTrip != null) {
      return 'Corrida atual';
    }

    return online ? 'Voce esta online' : 'Voce esta offline';
  }
}

class _OfferSummary extends StatelessWidget {
  const _OfferSummary({required this.offer});

  final Map<String, dynamic> offer;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '${offer['origin'] ?? offer['Origin']}',
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 4),
        Text('${offer['destination'] ?? offer['Destination']}'),
        const SizedBox(height: 8),
        Text('Ate a origem: ${offer['distanceKm'] ?? '-'} km'),
        Text('Tempo estimado: ${offer['etaMinutes'] ?? '-'} min'),
      ],
    );
  }
}

class _TripSummary extends StatelessWidget {
  const _TripSummary({
    required this.trip,
    required this.actualDistance,
    required this.actualDuration,
  });

  final Map<String, dynamic> trip;
  final TextEditingController actualDistance;
  final TextEditingController actualDuration;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          '${trip['origin'] ?? trip['Origin']}',
          style: Theme.of(context).textTheme.titleMedium,
        ),
        const SizedBox(height: 4),
        Text('${trip['destination'] ?? trip['Destination']}'),
        const SizedBox(height: 10),
        Row(
          children: [
            Expanded(
              child: _Input(controller: actualDistance, label: 'Km final'),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _Input(controller: actualDuration, label: 'Min final'),
            ),
          ],
        ),
      ],
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
      title: 'Localizacao',
      child: Column(
        children: [
          Text(
            streaming
                ? 'Enviando localizacao automaticamente.'
                : 'Ao ficar online, o app envia a localizacao automaticamente.',
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(child: _Input(controller: latitude, label: 'Latitude')),
              const SizedBox(width: 12),
              Expanded(
                  child: _Input(controller: longitude, label: 'Longitude')),
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
                  label: const Text('Enviar localizacao'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onToggleStream,
                  icon: Icon(streaming ? Icons.pause : Icons.play_arrow),
                  label:
                      Text(streaming ? 'Parar automatico' : 'Ligar automatico'),
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
    return Card(
      child: ExpansionTile(
        leading: const Icon(Icons.bug_report),
        title: const Text('Debug'),
        children: [
          DecoratedBox(
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
          const SizedBox(height: 12),
        ],
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
