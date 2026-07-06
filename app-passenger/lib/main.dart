import 'dart:async';
import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:geolocator/geolocator.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:signalr_netcore/signalr_client.dart' hide ConnectionState;

const defaultApiBaseUrl = 'http://45.185.199.173:8282';
const rideDark = Color(0xff07111f);
const ridePanel = Color(0xff101828);
const rideGold = Color(0xffffc928);
const rideGreen = Color(0xff16a34a);
const rideRed = Color(0xffdc2626);
const rideSurface = Color(0xffffffff);
const rideMuted = Color(0xff667085);

class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'RIDEPR_API_URL',
    defaultValue: defaultApiBaseUrl,
  );
}

void main() {
  runApp(const RidePrMvpTestApp());
}

class RidePrMvpTestApp extends StatelessWidget {
  const RidePrMvpTestApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'RidePR Passageiro',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: rideGold,
          primary: rideGold,
          secondary: rideGreen,
          error: rideRed,
        ),
        scaffoldBackgroundColor: const Color(0xfff4f6f8),
        useMaterial3: true,
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: rideGold,
            foregroundColor: rideDark,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: rideDark,
            side: const BorderSide(color: rideGold, width: 1.4),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: const Color(0xfff2f4f7),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: BorderSide.none,
          ),
        ),
      ),
      home: const MvpTestHome(),
    );
  }
}

class ApiClient {
  ApiClient({required this.baseUrl});

  String baseUrl;
  String? accessToken;

  Uri _uri(String path) {
    final normalizedBase = baseUrl.trim().replaceAll(RegExp(r'/+$'), '');
    final normalizedPath = path.startsWith('/') ? path : '/$path';

    return Uri.parse('$normalizedBase$normalizedPath');
  }

  Future<ApiResult> get(String path) async {
    return _send(() => http.get(_uri(path), headers: _headers()));
  }

  Future<ApiResult> post(String path, Map<String, dynamic> body) async {
    return _send(
      () => http.post(
        _uri(path),
        headers: _headers(),
        body: jsonEncode(body),
      ),
    );
  }

  Future<ApiResult> put(String path, Map<String, dynamic> body) async {
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

  Future<ApiResult> _send(Future<http.Response> Function() request) async {
    final response = await request();
    final body = response.body.trim();
    dynamic jsonBody;

    if (body.isNotEmpty) {
      try {
        jsonBody = jsonDecode(body);
      } catch (_) {
        jsonBody = body;
      }
    }

    return ApiResult(
      statusCode: response.statusCode,
      body: jsonBody,
      rawBody: body,
    );
  }
}

class ApiResult {
  ApiResult({
    required this.statusCode,
    required this.body,
    required this.rawBody,
  });

  final int statusCode;
  final dynamic body;
  final String rawBody;

  bool get success => statusCode >= 200 && statusCode < 300;

  String pretty() {
    final payload = body ?? rawBody;

    if (payload == null || payload == '') {
      return 'HTTP $statusCode\n(no body)';
    }

    const encoder = JsonEncoder.withIndent('  ');
    final formatted = payload is String ? payload : encoder.convert(payload);

    return 'HTTP $statusCode\n$formatted';
  }
}

class _RidePrLogo extends StatelessWidget {
  const _RidePrLogo();

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          height: 46,
          width: 46,
          decoration: BoxDecoration(
            color: rideGold,
            borderRadius: BorderRadius.circular(12),
          ),
          child: const Icon(Icons.local_taxi, color: rideDark, size: 28),
        ),
        const SizedBox(width: 12),
        Text(
          'RidePR',
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                color: rideDark,
                fontWeight: FontWeight.w900,
              ),
        ),
      ],
    );
  }
}

class LocalValidationException implements Exception {
  LocalValidationException(this.message);

  final String message;

  @override
  String toString() => message;
}

class MvpTestHome extends StatefulWidget {
  const MvpTestHome({super.key});

  @override
  State<MvpTestHome> createState() => _MvpTestHomeState();
}

class _MvpTestHomeState extends State<MvpTestHome> {
  final baseUrlController = TextEditingController(text: AppConfig.apiBaseUrl);
  final nameController = TextEditingController();
  final emailController = TextEditingController();
  final passwordController = TextEditingController();
  final confirmPasswordController = TextEditingController();
  final passengerIdController = TextEditingController();
  final passengerCpfController = TextEditingController();
  final passengerBirthDateController = TextEditingController();
  final passengerPhoneController = TextEditingController();
  final passengerEmergencyPhoneController = TextEditingController();
  final passengerAddressController = TextEditingController();
  final passengerCityController = TextEditingController();
  final passengerStateController = TextEditingController();
  final passengerZipCodeController = TextEditingController();
  final driverIdController = TextEditingController();
  final tripIdController = TextEditingController();
  final originController = TextEditingController();
  final destinationController = TextEditingController();
  final originLatController = TextEditingController();
  final originLngController = TextEditingController();
  final destinationLatController = TextEditingController();
  final destinationLngController = TextEditingController();
  final radiusController = TextEditingController(text: '5');
  final actualDistanceController = TextEditingController(text: '4.2');
  final actualDurationController = TextEditingController(text: '18');

  late final ApiClient api = ApiClient(baseUrl: baseUrlController.text);
  final mapController = MapController();
  HubConnection? hubConnection;
  Timer? destinationSearchTimer;
  String? connectedHubUrl;
  bool loading = false;
  bool restoring = true;
  bool debugVisible = false;
  String? accessToken;
  String liveStatus = 'SignalR desconectado.';
  String tripStatus = 'Sem corrida.';
  String lastResponse = 'Nenhuma chamada executada ainda.';
  String? userId;
  Map<String, dynamic>? passenger;
  LatLng? originPoint;
  LatLng? destinationPoint;
  LatLng? driverPoint;
  LatLng? lastCenteredPoint;
  String? locationError;
  List<Map<String, dynamic>> destinationSuggestions = [];

  bool get loggedIn => accessToken != null && accessToken!.isNotEmpty;
  bool get passengerReady =>
      passenger != null &&
      '${_field(passenger!, 'active')}'.toLowerCase() != 'false';
  bool get hasTripId => tripIdController.text.trim().isNotEmpty;

  @override
  void initState() {
    super.initState();
    tripIdController.addListener(_refreshActionState);
    driverIdController.addListener(_refreshActionState);
    destinationController.addListener(_searchDestination);
    _restoreSession();
  }

  @override
  void dispose() {
    hubConnection?.stop();
    destinationSearchTimer?.cancel();
    tripIdController.removeListener(_refreshActionState);
    driverIdController.removeListener(_refreshActionState);
    destinationController.removeListener(_searchDestination);
    baseUrlController.dispose();
    nameController.dispose();
    emailController.dispose();
    passwordController.dispose();
    confirmPasswordController.dispose();
    passengerIdController.dispose();
    passengerCpfController.dispose();
    passengerBirthDateController.dispose();
    passengerPhoneController.dispose();
    passengerEmergencyPhoneController.dispose();
    passengerAddressController.dispose();
    passengerCityController.dispose();
    passengerStateController.dispose();
    passengerZipCodeController.dispose();
    driverIdController.dispose();
    tripIdController.dispose();
    originController.dispose();
    destinationController.dispose();
    originLatController.dispose();
    originLngController.dispose();
    destinationLatController.dispose();
    destinationLngController.dispose();
    radiusController.dispose();
    actualDistanceController.dispose();
    actualDurationController.dispose();
    super.dispose();
  }

  void _refreshActionState() {
    if (mounted) {
      setState(() {});
    }
  }

  @override
  Widget build(BuildContext context) {
    if (restoring) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    if (!loggedIn) {
      return _PassengerAuthPage(
        nameController: nameController,
        emailController: emailController,
        passwordController: passwordController,
        confirmPasswordController: confirmPasswordController,
        loading: loading,
        lastResponse: lastResponse,
        debugVisible: debugVisible,
        onLogin: _login,
        onRegister: _register,
        onToggleDebug: () => setState(() => debugVisible = !debugVisible),
      );
    }

    if (!passengerReady) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Complete seu perfil'),
          actions: [
            IconButton(
              onPressed: () => _clearSession(),
              icon: const Icon(Icons.logout),
              tooltip: 'Sair',
            ),
          ],
        ),
        body: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              'Complete seus dados para pedir uma corrida.',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: 16),
            _Input(controller: nameController, label: 'Nome'),
            _Input(controller: passengerCpfController, label: 'CPF'),
            _Input(controller: passengerPhoneController, label: 'Telefone'),
            _Input(
              controller: passengerBirthDateController,
              label: 'Nascimento (AAAA-MM-DD)',
            ),
            _Input(controller: passengerAddressController, label: 'Endereco'),
            Row(
              children: [
                Expanded(
                  child: _Input(
                    controller: passengerCityController,
                    label: 'Cidade',
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _Input(
                    controller: passengerStateController,
                    label: 'UF',
                  ),
                ),
              ],
            ),
            _Input(controller: passengerZipCodeController, label: 'CEP'),
            const SizedBox(height: 8),
            FilledButton.icon(
              onPressed: loading ? null : () => _savePassenger(),
              icon: const Icon(Icons.save),
              label: const Text('Salvar e continuar'),
            ),
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
              const _RidePrLogo(),
              const SizedBox(height: 16),
              ListTile(
                leading: const Icon(Icons.person),
                title: const Text('Perfil'),
                subtitle: const Text('Dados pessoais e endereco'),
                onTap: () {
                  Navigator.of(context).pop();
                  _openProfile();
                },
              ),
              ListTile(
                leading: const Icon(Icons.history),
                title: const Text('Minhas corridas'),
                subtitle: const Text('Historico do passageiro'),
                onTap: () {
                  Navigator.of(context).pop();
                  _openHistory();
                },
              ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                onPressed: () => _clearSession(),
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
                  userAgentPackageName: 'com.ridepr.passenger',
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
                      heroTag: 'menu',
                      backgroundColor: rideDark,
                      foregroundColor: Colors.white,
                      onPressed: () => Scaffold.of(context).openEndDrawer(),
                      child: const Icon(Icons.menu),
                    ),
                  ),
                  const Spacer(),
                  DecoratedBox(
                    decoration: BoxDecoration(
                      color: rideDark,
                      borderRadius: BorderRadius.circular(999),
                      boxShadow: const [
                        BoxShadow(color: Colors.black26, blurRadius: 12),
                      ],
                    ),
                    child: Padding(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 12,
                        vertical: 8,
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            loggedIn ? Icons.check_circle : Icons.login,
                            size: 18,
                            color: loggedIn ? rideGreen : rideGold,
                          ),
                          const SizedBox(width: 6),
                          Text(
                            loggedIn ? 'Conectado' : 'Entrar',
                            style: const TextStyle(color: Colors.white),
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Align(
            alignment: Alignment.bottomCenter,
            child: _PassengerRideCard(
              loggedIn: loggedIn,
              loading: loading,
              passengerReady: passengerReady,
              tripStatus: tripStatus,
              liveStatus: liveStatus,
              locationError: locationError,
              originController: originController,
              destinationController: destinationController,
              destinationSuggestions: destinationSuggestions,
              onUseMyLocation: _useCurrentLocation,
              onSelectDestination: _selectDestination,
              onLogin: _login,
              onSavePassenger: _savePassenger,
              onCreateTrip: _createTrip,
              onRefresh: hasTripId ? _getTrip : null,
            ),
          ),
        ],
      ),
    );
  }

  List<Marker> _markers() {
    return [
      if (originPoint != null)
        _mapMarker(originPoint!, Icons.trip_origin, Colors.green),
      if (destinationPoint != null)
        _mapMarker(destinationPoint!, Icons.flag, Colors.red),
      if (driverPoint != null)
        _mapMarker(driverPoint!, Icons.local_taxi, Colors.blue),
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

  Future<void> _login() async {
    await _run(() async {
      api.baseUrl = baseUrlController.text;
      final result = await api.post('/api/auth/login', {
        'email': emailController.text.trim(),
        'password': passwordController.text,
      });

      final token = result.body is Map<String, dynamic>
          ? result.body['accessToken'] as String?
          : null;
      final loggedUserId = result.body is Map<String, dynamic>
          ? result.body['userId'] as String?
          : null;

      if (result.success && token != null) {
        accessToken = token;
        api.accessToken = token;
        userId = loggedUserId;
        await _rememberSession(result.body as Map<String, dynamic>);
        await _loadPassenger();
        await _connectRealtime();
        await _useCurrentLocation();
      }

      return result;
    });
  }

  Future<void> _register() async {
    await _run(() async {
      if (passwordController.text != confirmPasswordController.text) {
        throw LocalValidationException('As senhas nao conferem.');
      }

      api.baseUrl = baseUrlController.text;
      final result = await api.post('/api/auth/register', {
        'name': nameController.text.trim().isEmpty
            ? 'Passageiro RidePR'
            : nameController.text.trim(),
        'email': emailController.text.trim(),
        'password': passwordController.text,
        'role': 1,
      });

      final token = result.body is Map<String, dynamic>
          ? result.body['accessToken'] as String?
          : null;
      final loggedUserId = result.body is Map<String, dynamic>
          ? result.body['userId'] as String?
          : null;

      if (result.success && token != null) {
        accessToken = token;
        api.accessToken = token;
        userId = loggedUserId;
        await _rememberSession(result.body as Map<String, dynamic>);
        await _loadPassenger();
        await _connectRealtime();
        await _useCurrentLocation();
      }

      return result;
    });
  }

  Future<void> _restoreSession() async {
    baseUrlController.text = AppConfig.apiBaseUrl;
    api.baseUrl = baseUrlController.text;

    if (mounted) {
      setState(() => restoring = false);
    }
  }

  Future<void> _rememberSession(Map<String, dynamic> data) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('accessToken', accessToken ?? '');
    await prefs.setString('userId', userId ?? '');
    await prefs.setString('name', '${data['name'] ?? ''}');
    await prefs.setString('email', '${data['email'] ?? ''}');
  }

  Future<void> _createTrip() async {
    await _run(() async {
      _requireLoggedIn();
      _requirePassenger();
      if (_doubleValue(originLatController) == 0 ||
          _doubleValue(originLngController) == 0) {
        throw LocalValidationException(
          'Use sua localizacao atual antes de pedir a corrida.',
        );
      }
      if (_doubleValue(destinationLatController) == 0 ||
          _doubleValue(destinationLngController) == 0) {
        throw LocalValidationException(
          'Escolha um destino valido na busca.',
        );
      }
      final result = await api.post('/api/trips', {
        'passengerId': passengerIdController.text.trim(),
        'origin': originController.text.trim(),
        'destination': destinationController.text.trim(),
        'originLatitude': _doubleValue(originLatController),
        'originLongitude': _doubleValue(originLngController),
        'destinationLatitude': _doubleValue(destinationLatController),
        'destinationLongitude': _doubleValue(destinationLngController),
      });

      final trip = result.body is Map<String, dynamic>
          ? result.body as Map<String, dynamic>
          : null;
      final tripId = trip?['id'] as String?;

      if (result.success && trip != null && tripId != null) {
        tripIdController.text = tripId;
        _applyTrip(trip, eventName: 'TripRequested');
        await api.post('/api/dispatch/request', {
          'tripId': tripId,
          'radiusKm': _doubleValue(radiusController),
          'timeoutSeconds': 30,
          'maxCandidates': 10,
        });
        setState(() => tripStatus = 'Procurando motorista');
      }

      return result;
    });
  }

  Future<void> _loadPassenger() async {
    if (userId == null || userId!.isEmpty) {
      return;
    }

    final result = await api.get('/api/passengers/by-user/$userId');

    if (!result.success || result.body is! Map<String, dynamic>) {
      passenger = null;
      return;
    }

    passenger = result.body as Map<String, dynamic>;
    _fillPassengerForm(passenger!);
  }

  Future<void> _savePassenger() async {
    await _run(() async {
      _requireLoggedIn();

      if (userId == null || userId!.isEmpty) {
        throw LocalValidationException(
            'Faca login novamente para salvar o passageiro.');
      }

      final body = {
        'userId': userId,
        'cpf': passengerCpfController.text.trim(),
        'birthDate': _dateIso(passengerBirthDateController),
        'phone': passengerPhoneController.text.trim(),
        'emergencyPhone': passengerEmergencyPhoneController.text.trim(),
        'address': passengerAddressController.text.trim(),
        'city': passengerCityController.text.trim(),
        'state': passengerStateController.text.trim(),
        'zipCode': passengerZipCodeController.text.trim(),
        'active': true,
      };
      final result = passenger == null
          ? await api.post('/api/passengers', body)
          : await api.put('/api/passengers/${_field(passenger!, 'id')}', body);

      if (result.success && result.body is Map<String, dynamic>) {
        passenger = result.body as Map<String, dynamic>;
        _fillPassengerForm(passenger!);
      }

      return result;
    });
  }

  Future<void> _useCurrentLocation() async {
    final position = await _readCurrentPosition();

    if (position == null) {
      return;
    }

    final point = LatLng(position.latitude, position.longitude);
    originPoint = point;
    originLatController.text = point.latitude.toStringAsFixed(6);
    originLngController.text = point.longitude.toStringAsFixed(6);

    try {
      final result = await api.post('/api/maps/reverse-geocode', {
        'coordinate': {
          'latitude': point.latitude,
          'longitude': point.longitude,
        },
      });

      if (result.success && result.body is Map<String, dynamic>) {
        originController.text =
            '${(result.body as Map<String, dynamic>)['address'] ?? 'Minha localizacao'}';
      } else {
        originController.text = 'Minha localizacao';
      }
    } catch (_) {
      originController.text = 'Minha localizacao';
    }

    setState(() {
      locationError = null;
      lastCenteredPoint = point;
    });
    mapController.move(point, 15);
  }

  Future<Position?> _readCurrentPosition() async {
    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();

      if (!serviceEnabled) {
        setState(() => locationError = 'GPS desligado. Ative a localizacao.');
        return null;
      }

      var permission = await Geolocator.checkPermission();

      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }

      if (permission == LocationPermission.denied) {
        setState(() => locationError = 'Permissao de localizacao negada.');
        return null;
      }

      if (permission == LocationPermission.deniedForever) {
        setState(
          () => locationError =
              'Permissao bloqueada. Libere a localizacao nas configuracoes.',
        );
        return null;
      }

      return Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } catch (ex) {
      setState(() => locationError = _friendlyError('$ex'));
      return null;
    }
  }

  void _searchDestination() {
    destinationSearchTimer?.cancel();
    final query = destinationController.text.trim();

    if (!loggedIn || query.length < 4) {
      if (destinationSuggestions.isNotEmpty) {
        setState(() => destinationSuggestions = []);
      }
      return;
    }

    destinationSearchTimer = Timer(const Duration(milliseconds: 450), () async {
      try {
        final result = await api.post('/api/maps/geocode', {'address': query});

        if (!mounted ||
            !result.success ||
            result.body is! Map<String, dynamic>) {
          return;
        }

        setState(() {
          destinationSuggestions = [result.body as Map<String, dynamic>];
        });
      } catch (_) {
        if (mounted) {
          setState(() => destinationSuggestions = []);
        }
      }
    });
  }

  void _selectDestination(Map<String, dynamic> suggestion) {
    destinationController.removeListener(_searchDestination);
    destinationController.text = '${suggestion['address'] ?? ''}';
    destinationController.addListener(_searchDestination);

    final point = LatLng(
      _numField(suggestion, 'latitude'),
      _numField(suggestion, 'longitude'),
    );

    setState(() {
      destinationPoint = point;
      destinationLatController.text = point.latitude.toStringAsFixed(6);
      destinationLngController.text = point.longitude.toStringAsFixed(6);
      destinationSuggestions = [];
    });
    _moveMapToVisiblePoint();
  }

  Future<void> _getTrip() async {
    await _run(() async {
      _requireLoggedIn();
      _requireTripId();
      final result =
          await api.get('/api/trips/${tripIdController.text.trim()}');

      if (result.success && result.body is Map<String, dynamic>) {
        _applyTrip(result.body as Map<String, dynamic>, eventName: 'Status');
      }

      return result;
    });
  }

  Future<void> _connectRealtime() async {
    final hubUrl =
        '${baseUrlController.text.trim().replaceAll(RegExp(r'/+$'), '')}/driverHub';

    if (connectedHubUrl == hubUrl &&
        hubConnection?.state == HubConnectionState.Connected) {
      return;
    }

    await hubConnection?.stop();

    final options = HttpConnectionOptions(
      accessTokenFactory: () async => accessToken ?? '',
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

    void onTrip(String eventName, List<Object?>? args) {
      final trip = args?.isNotEmpty == true && args!.first is Map
          ? Map<String, dynamic>.from(args.first! as Map)
          : null;

      if (trip != null && mounted) {
        _applyTrip(trip, eventName: eventName);
        setState(() {
          lastResponse = _prettyRealtime(eventName, trip);
        });
      }
    }

    connection.on('TripRequested', (args) => onTrip('TripRequested', args));
    connection.on('TripAccepted', (args) => onTrip('TripAccepted', args));
    connection.on('TripStarted', (args) => onTrip('TripStarted', args));
    connection.on('TripFinished', (args) => onTrip('TripFinished', args));
    connection.on('DriverLocationUpdated', (args) {
      final location = args?.isNotEmpty == true && args!.first is Map
          ? Map<String, dynamic>.from(args.first! as Map)
          : null;

      if (location == null || !mounted) {
        return;
      }

      final nextPoint = LatLng(
        _numField(location, 'latitude'),
        _numField(location, 'longitude'),
      );

      if (_samePoint(driverPoint, nextPoint)) {
        return;
      }

      setState(() {
        driverPoint = nextPoint;
        liveStatus = 'Localizacao do motorista atualizada';
        lastResponse = _prettyRealtime('DriverLocationUpdated', location);
      });
      _moveMapToVisiblePoint();
    });

    await connection.start();

    if (mounted) {
      setState(() {
        hubConnection = connection;
        connectedHubUrl = hubUrl;
        liveStatus = 'SignalR conectado.';
      });
    }
  }

  Future<void> _run(Future<ApiResult> Function() action) async {
    setState(() => loading = true);

    try {
      final result = await action();

      setState(() => lastResponse = result.pretty());

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              result.success
                  ? 'Tudo certo. Operacao concluida.'
                  : 'Nao foi possivel concluir. Veja o Debug.',
            ),
          ),
        );
      }
    } catch (error) {
      setState(() => lastResponse = 'Erro local\n$error');
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(_friendlyError('$error'))),
        );
      }
    } finally {
      if (mounted) {
        setState(() => loading = false);
      }
    }
  }

  void _applyTrip(Map<String, dynamic> trip, {required String eventName}) {
    setState(() {
      tripIdController.text = '${_field(trip, 'id') ?? tripIdController.text}';
      tripStatus =
          '${_eventLabel(eventName)}: ${_statusLabel(_field(trip, 'status'))}';
      originPoint = LatLng(
        _numField(trip, 'originLatitude'),
        _numField(trip, 'originLongitude'),
      );
      destinationPoint = LatLng(
        _numField(trip, 'destinationLatitude'),
        _numField(trip, 'destinationLongitude'),
      );
    });
    _moveMapToVisiblePoint();
  }

  void _moveMapToVisiblePoint() {
    final point = driverPoint ?? originPoint ?? destinationPoint;

    if (point != null && !_samePoint(lastCenteredPoint, point)) {
      lastCenteredPoint = point;
      mapController.move(point, 14);
    }
  }

  Future<void> _clearSession() async {
    await hubConnection?.stop();
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
    setState(() {
      accessToken = null;
      api.accessToken = null;
      userId = null;
      passenger = null;
      hubConnection = null;
      connectedHubUrl = null;
      liveStatus = 'SignalR desconectado.';
      lastResponse = 'Token removido da memoria.';
    });
  }

  void _openProfile() {
    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => Scaffold(
          appBar: AppBar(title: const Text('Perfil')),
          body: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              _Input(controller: nameController, label: 'Nome'),
              _Input(controller: passengerCpfController, label: 'CPF'),
              _Input(controller: passengerPhoneController, label: 'Telefone'),
              _Input(
                controller: passengerBirthDateController,
                label: 'Nascimento (AAAA-MM-DD)',
              ),
              _Input(controller: passengerAddressController, label: 'Endereco'),
              Row(
                children: [
                  Expanded(
                    child: _Input(
                      controller: passengerCityController,
                      label: 'Cidade',
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _Input(
                      controller: passengerStateController,
                      label: 'UF',
                    ),
                  ),
                ],
              ),
              _Input(controller: passengerZipCodeController, label: 'CEP'),
              const SizedBox(height: 8),
              FilledButton.icon(
                onPressed: loading ? null : () => _savePassenger(),
                icon: const Icon(Icons.save),
                label: const Text('Salvar'),
              ),
              const SizedBox(height: 8),
              OutlinedButton.icon(
                onPressed: () => _clearSession(),
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
    final currentPassengerId = passengerIdController.text.trim();

    if (currentPassengerId.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Complete seu perfil primeiro.')),
      );
      return;
    }

    Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => _PassengerHistoryPage(
          api: api,
          passengerId: currentPassengerId,
        ),
      ),
    );
  }

  static bool _samePoint(LatLng? left, LatLng? right) {
    if (left == null || right == null) {
      return false;
    }

    return (left.latitude - right.latitude).abs() < 0.00001 &&
        (left.longitude - right.longitude).abs() < 0.00001;
  }

  static String _friendlyError(String message) {
    if (message.contains('SocketException') ||
        message.contains('Connection refused') ||
        message.contains('Failed host lookup')) {
      return 'Nao consegui conectar na API. Confira internet, IP e porta.';
    }

    if (message.contains('401') || message.contains('Unauthorized')) {
      return 'Sessao expirada ou login invalido. Entre novamente.';
    }

    return message.replaceFirst('Exception: ', '');
  }

  static Object? _field(Map<String, dynamic> source, String name) {
    final pascal = name.substring(0, 1).toUpperCase() + name.substring(1);
    return source[name] ?? source[pascal];
  }

  static double _numField(Map<String, dynamic> source, String name) {
    final value = _field(source, name);

    if (value is num) {
      return value.toDouble();
    }

    return double.tryParse('$value') ?? 0;
  }

  static double _doubleValue(TextEditingController controller) {
    return double.tryParse(controller.text.trim().replaceAll(',', '.')) ?? 0;
  }

  void _requireLoggedIn() {
    if (!loggedIn) {
      throw LocalValidationException('Faca login antes de continuar.');
    }
  }

  void _requireTripId() {
    if (!hasTripId) {
      throw LocalValidationException('Crie uma corrida ou informe o TripId.');
    }
  }

  void _requirePassenger() {
    if (passengerIdController.text.trim().isEmpty) {
      throw LocalValidationException(
          'Salve o cadastro do passageiro antes de criar corrida.');
    }
  }

  void _fillPassengerForm(Map<String, dynamic> source) {
    passengerIdController.text = '${_field(source, 'id') ?? ''}';
    passengerCpfController.text = '${_field(source, 'cpf') ?? ''}';
    passengerBirthDateController.text = _dateValue(source, 'birthDate');
    passengerPhoneController.text = '${_field(source, 'phone') ?? ''}';
    passengerEmergencyPhoneController.text =
        '${_field(source, 'emergencyPhone') ?? ''}';
    passengerAddressController.text = '${_field(source, 'address') ?? ''}';
    passengerCityController.text = '${_field(source, 'city') ?? ''}';
    passengerStateController.text = '${_field(source, 'state') ?? ''}';
    passengerZipCodeController.text = '${_field(source, 'zipCode') ?? ''}';
  }

  static String _dateValue(Map<String, dynamic> source, String key) {
    final value = '${_field(source, key) ?? ''}';

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

  static String _statusLabel(Object? value) {
    return switch ('$value') {
      '0' || 'Requested' => 'Solicitada',
      '1' || 'Accepted' => 'Aceita',
      '2' || 'InProgress' => 'Em andamento',
      '3' || 'Finished' => 'Finalizada',
      _ => '$value',
    };
  }

  static String _eventLabel(String value) {
    return switch (value) {
      'TripRequested' => 'Procurando motorista',
      'TripAccepted' => 'Motorista a caminho',
      'TripStarted' => 'Corrida em andamento',
      'TripFinished' => 'Corrida finalizada',
      'Status' => 'Status consultado',
      _ => value,
    };
  }

  static String _prettyRealtime(String eventName, Map<String, dynamic> data) {
    const encoder = JsonEncoder.withIndent('  ');
    return 'SIGNALR $eventName\n${encoder.convert(data)}';
  }
}

class _PassengerAuthPage extends StatefulWidget {
  const _PassengerAuthPage({
    required this.nameController,
    required this.emailController,
    required this.passwordController,
    required this.confirmPasswordController,
    required this.loading,
    required this.lastResponse,
    required this.debugVisible,
    required this.onLogin,
    required this.onRegister,
    required this.onToggleDebug,
  });

  final TextEditingController nameController;
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final TextEditingController confirmPasswordController;
  final bool loading;
  final String lastResponse;
  final bool debugVisible;
  final VoidCallback onLogin;
  final VoidCallback onRegister;
  final VoidCallback onToggleDebug;

  @override
  State<_PassengerAuthPage> createState() => _PassengerAuthPageState();
}

class _PassengerAuthPageState extends State<_PassengerAuthPage> {
  String mode = 'entry';

  TextEditingController get nameController => widget.nameController;
  TextEditingController get emailController => widget.emailController;
  TextEditingController get passwordController => widget.passwordController;
  TextEditingController get confirmPasswordController =>
      widget.confirmPasswordController;
  bool get loading => widget.loading;
  String get lastResponse => widget.lastResponse;
  bool get debugVisible => widget.debugVisible;
  VoidCallback get onLogin => widget.onLogin;
  VoidCallback get onRegister => widget.onRegister;
  VoidCallback get onToggleDebug => widget.onToggleDebug;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: rideDark,
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 430),
            child: Container(
              margin: const EdgeInsets.all(20),
              padding: const EdgeInsets.all(22),
              decoration: BoxDecoration(
                color: rideSurface,
                borderRadius: BorderRadius.circular(8),
                boxShadow: const [
                  BoxShadow(
                    color: Color(0x66000000),
                    blurRadius: 28,
                    offset: Offset(0, 18),
                  ),
                ],
              ),
              child: ListView(
                shrinkWrap: true,
                children: [
                  const _RidePrLogo(),
                  const SizedBox(height: 14),
                  Text(
                    mode == 'register'
                        ? 'Crie sua conta de passageiro.'
                        : mode == 'login'
                            ? 'Entre para pedir sua corrida.'
                            : 'Peça corridas de forma simples.',
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: rideMuted,
                        ),
                  ),
                  const SizedBox(height: 24),
                  if (mode == 'register')
                    _Input(
                      controller: nameController,
                      label: 'Nome',
                    ),
                  if (mode != 'entry')
                    _Input(controller: emailController, label: 'E-mail'),
                  if (mode != 'entry')
                    _Input(
                      controller: passwordController,
                      label: 'Senha',
                      obscureText: true,
                    ),
                  if (mode == 'register')
                    _Input(
                      controller: confirmPasswordController,
                      label: 'Confirmar senha',
                      obscureText: true,
                    ),
                  const SizedBox(height: 8),
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
                              ? onRegister
                              : onLogin,
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
      ),
    );
  }
}

class _PassengerHistoryPage extends StatefulWidget {
  const _PassengerHistoryPage({
    required this.api,
    required this.passengerId,
  });

  final ApiClient api;
  final String passengerId;

  @override
  State<_PassengerHistoryPage> createState() => _PassengerHistoryPageState();
}

class _PassengerHistoryPageState extends State<_PassengerHistoryPage> {
  late Future<List<Map<String, dynamic>>> trips = _load();

  Future<List<Map<String, dynamic>>> _load() async {
    final result = await widget.api.get('/api/trips');
    final list = result.body is List ? result.body as List : <dynamic>[];

    return list
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .where(
          (trip) =>
              '${trip['passengerId'] ?? trip['PassengerId']}'.toLowerCase() ==
              widget.passengerId.toLowerCase(),
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
            return const Center(
              child: Text('Nao foi possivel carregar o historico.'),
            );
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
              itemCount: rows.length,
              separatorBuilder: (_, __) => const SizedBox(height: 10),
              itemBuilder: (context, index) {
                final trip = rows[index];
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
                    '${_dateLabel(trip['createdAt'] ?? trip['CreatedAt'])} • '
                    '${_statusLabel(trip['status'] ?? trip['Status'])}',
                  ),
                  isThreeLine: true,
                  trailing: Text(price == null ? '-' : 'R\$ $price'),
                );
              },
            ),
          );
        },
      ),
    );
  }

  static String _dateLabel(Object? value) {
    final parsed = DateTime.tryParse('$value')?.toLocal();

    if (parsed == null) {
      return 'Sem data';
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

// ignore: unused_element
class _CreateTripScreen extends StatelessWidget {
  const _CreateTripScreen({
    required this.passengerCpfController,
    required this.passengerBirthDateController,
    required this.passengerPhoneController,
    required this.passengerEmergencyPhoneController,
    required this.passengerAddressController,
    required this.passengerCityController,
    required this.passengerStateController,
    required this.passengerZipCodeController,
    required this.passengerIdController,
    required this.tripIdController,
    required this.originController,
    required this.destinationController,
    required this.originLatController,
    required this.originLngController,
    required this.destinationLatController,
    required this.destinationLngController,
    required this.loading,
    required this.canCreateTrip,
    required this.passengerLoaded,
    required this.onSavePassenger,
    required this.onCreateTrip,
  });

  final TextEditingController passengerCpfController;
  final TextEditingController passengerBirthDateController;
  final TextEditingController passengerPhoneController;
  final TextEditingController passengerEmergencyPhoneController;
  final TextEditingController passengerAddressController;
  final TextEditingController passengerCityController;
  final TextEditingController passengerStateController;
  final TextEditingController passengerZipCodeController;
  final TextEditingController passengerIdController;
  final TextEditingController tripIdController;
  final TextEditingController originController;
  final TextEditingController destinationController;
  final TextEditingController originLatController;
  final TextEditingController originLngController;
  final TextEditingController destinationLatController;
  final TextEditingController destinationLngController;
  final bool loading;
  final bool canCreateTrip;
  final bool passengerLoaded;
  final VoidCallback onSavePassenger;
  final VoidCallback onCreateTrip;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Pedir corrida',
      children: [
        Text(passengerLoaded
            ? 'Cadastro pronto. Confira origem e destino.'
            : 'Complete seu cadastro uma vez para pedir corrida.'),
        ExpansionTile(
          title: const Text('Meu cadastro'),
          initiallyExpanded: !passengerLoaded,
          children: [
            _Input(controller: passengerCpfController, label: 'CPF'),
            _Input(
              controller: passengerBirthDateController,
              label: 'Nascimento (AAAA-MM-DD)',
            ),
            _Input(controller: passengerPhoneController, label: 'Telefone'),
            _Input(
              controller: passengerEmergencyPhoneController,
              label: 'Telefone emergencia',
            ),
            _Input(controller: passengerAddressController, label: 'Endereco'),
            Row(
              children: [
                Expanded(
                    child: _Input(
                        controller: passengerCityController, label: 'Cidade')),
                const SizedBox(width: 8),
                Expanded(
                    child: _Input(
                        controller: passengerStateController, label: 'UF')),
                const SizedBox(width: 8),
                Expanded(
                    child: _Input(
                        controller: passengerZipCodeController, label: 'CEP')),
              ],
            ),
            FilledButton.icon(
              onPressed: loading || !canCreateTrip ? null : onSavePassenger,
              icon: const Icon(Icons.person),
              label: Text(passengerLoaded ? 'Salvar cadastro' : 'Cadastrar'),
            ),
          ],
        ),
        _Input(controller: originController, label: 'Origem'),
        _Input(controller: destinationController, label: 'Destino'),
        ExpansionTile(
          title: const Text('Avancado'),
          children: [
            _Input(
                controller: passengerIdController,
                label: 'Codigo do passageiro'),
            _Input(controller: tripIdController, label: 'Codigo da corrida'),
            Row(
              children: [
                Expanded(
                  child: _Input(
                      controller: originLatController, label: 'Lat origem'),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _Input(
                      controller: originLngController, label: 'Lng origem'),
                ),
              ],
            ),
            Row(
              children: [
                Expanded(
                  child: _Input(
                    controller: destinationLatController,
                    label: 'Lat destino',
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _Input(
                    controller: destinationLngController,
                    label: 'Lng destino',
                  ),
                ),
              ],
            ),
          ],
        ),
        FilledButton.icon(
          onPressed: loading || !canCreateTrip ? null : onCreateTrip,
          icon: const Icon(Icons.local_taxi),
          label: const Text('Pedir corrida'),
        ),
      ],
    );
  }
}

// ignore: unused_element
class _TestButtonsScreen extends StatelessWidget {
  const _TestButtonsScreen({
    required this.tripIdController,
    required this.driverIdController,
    required this.radiusController,
    required this.actualDistanceController,
    required this.actualDurationController,
    required this.loading,
    required this.canRequestDispatch,
    required this.canDriverAction,
    required this.onRequestDispatch,
    required this.onAccept,
    required this.onStart,
    required this.onFinish,
  });

  final TextEditingController tripIdController;
  final TextEditingController driverIdController;
  final TextEditingController radiusController;
  final TextEditingController actualDistanceController;
  final TextEditingController actualDurationController;
  final bool loading;
  final bool canRequestDispatch;
  final bool canDriverAction;
  final VoidCallback onRequestDispatch;
  final VoidCallback onAccept;
  final VoidCallback onStart;
  final VoidCallback onFinish;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Botoes de teste',
      children: [
        _Input(controller: tripIdController, label: 'TripId'),
        _Input(controller: driverIdController, label: 'DriverId manual'),
        _Input(controller: radiusController, label: 'Raio dispatch km'),
        _Input(
          controller: actualDistanceController,
          label: 'Distancia final km',
        ),
        _Input(
          controller: actualDurationController,
          label: 'Duracao final minutos',
        ),
        FilledButton.icon(
          onPressed: loading || !canRequestDispatch ? null : onRequestDispatch,
          icon: const Icon(Icons.radar),
          label: const Text('Solicitar dispatch'),
        ),
        FilledButton.icon(
          onPressed: loading || !canDriverAction ? null : onAccept,
          icon: const Icon(Icons.check_circle),
          label: const Text('Aceitar corrida'),
        ),
        FilledButton.icon(
          onPressed: loading || !canDriverAction ? null : onStart,
          icon: const Icon(Icons.play_arrow),
          label: const Text('Iniciar corrida'),
        ),
        FilledButton.icon(
          onPressed: loading || !canDriverAction ? null : onFinish,
          icon: const Icon(Icons.flag),
          label: const Text('Finalizar corrida'),
        ),
      ],
    );
  }
}

class _PassengerRideCard extends StatelessWidget {
  const _PassengerRideCard({
    required this.loggedIn,
    required this.loading,
    required this.passengerReady,
    required this.tripStatus,
    required this.liveStatus,
    required this.locationError,
    required this.originController,
    required this.destinationController,
    required this.destinationSuggestions,
    required this.onUseMyLocation,
    required this.onSelectDestination,
    required this.onLogin,
    required this.onSavePassenger,
    required this.onCreateTrip,
    required this.onRefresh,
  });

  final bool loggedIn;
  final bool loading;
  final bool passengerReady;
  final String tripStatus;
  final String liveStatus;
  final String? locationError;
  final TextEditingController originController;
  final TextEditingController destinationController;
  final List<Map<String, dynamic>> destinationSuggestions;
  final VoidCallback onUseMyLocation;
  final ValueChanged<Map<String, dynamic>> onSelectDestination;
  final VoidCallback onLogin;
  final VoidCallback onSavePassenger;
  final VoidCallback onCreateTrip;
  final VoidCallback? onRefresh;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      top: false,
      child: Container(
        margin: const EdgeInsets.all(12),
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: rideSurface,
          borderRadius: BorderRadius.circular(8),
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
                Container(
                  height: 34,
                  width: 34,
                  decoration: BoxDecoration(
                    color: rideGold,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: const Icon(Icons.local_taxi, color: rideDark),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: Text(
                    _headline,
                    style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w800,
                          color: rideDark,
                        ),
                  ),
                ),
                if (onRefresh != null)
                  IconButton(
                    tooltip: 'Atualizar',
                    onPressed: loading ? null : onRefresh,
                    icon: const Icon(Icons.refresh),
                  ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: _RideInput(
                    controller: originController,
                    icon: Icons.trip_origin,
                    label: 'Origem',
                  ),
                ),
                const SizedBox(width: 8),
                IconButton.filledTonal(
                  tooltip: 'Minha localizacao',
                  onPressed: loading ? null : onUseMyLocation,
                  icon: const Icon(Icons.my_location),
                ),
              ],
            ),
            const SizedBox(height: 10),
            _RideInput(
              controller: destinationController,
              icon: Icons.flag,
              label: 'Destino',
            ),
            if (destinationSuggestions.isNotEmpty) ...[
              const SizedBox(height: 8),
              ...destinationSuggestions.map(
                (suggestion) => ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: const Icon(Icons.place),
                  title: Text('${suggestion['address'] ?? ''}'),
                  onTap: () => onSelectDestination(suggestion),
                ),
              ),
            ],
            const SizedBox(height: 12),
            Text(tripStatus, style: Theme.of(context).textTheme.titleMedium),
            if (locationError != null) ...[
              const SizedBox(height: 4),
              Text(
                locationError!,
                style: Theme.of(context)
                    .textTheme
                    .bodySmall
                    ?.copyWith(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 4),
            Text(
              liveStatus.toLowerCase().contains('conectado')
                  ? 'Atualizacao automatica ativa'
                  : 'Atualizando status da corrida',
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: rideMuted,
                  ),
            ),
            const SizedBox(height: 14),
            FilledButton(
              onPressed: loading ? null : _primaryAction,
              style: FilledButton.styleFrom(
                minimumSize: const Size.fromHeight(52),
                textStyle: const TextStyle(
                  fontSize: 17,
                  fontWeight: FontWeight.w700,
                ),
              ),
              child: Text(loading ? 'Aguarde...' : _buttonText),
            ),
          ],
        ),
      ),
    );
  }

  String get _headline {
    if (!loggedIn) {
      return 'Para onde vamos?';
    }

    if (!passengerReady) {
      return 'Complete seu cadastro';
    }

    return 'Escolha sua corrida';
  }

  String get _buttonText {
    if (!loggedIn) {
      return 'Entrar';
    }

    if (!passengerReady) {
      return 'Salvar cadastro';
    }

    return 'Pedir corrida';
  }

  VoidCallback get _primaryAction {
    if (!loggedIn) {
      return onLogin;
    }

    if (!passengerReady) {
      return onSavePassenger;
    }

    return onCreateTrip;
  }
}

class _RideInput extends StatelessWidget {
  const _RideInput({
    required this.controller,
    required this.icon,
    required this.label,
  });

  final TextEditingController controller;
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    return TextField(
      controller: controller,
      decoration: InputDecoration(
        filled: true,
        fillColor: const Color(0xfff2f4f7),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide.none,
        ),
        prefixIcon: Icon(icon),
        labelText: label,
      ),
    );
  }
}

class _ScreenFrame extends StatelessWidget {
  const _ScreenFrame({
    required this.title,
    required this.children,
  });

  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 180),
      children: [
        Text(title, style: Theme.of(context).textTheme.headlineSmall),
        const SizedBox(height: 16),
        ...children.expand((child) => [child, const SizedBox(height: 12)]),
      ],
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
    return TextField(
      controller: controller,
      obscureText: obscureText,
      decoration: InputDecoration(
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(8),
          borderSide: BorderSide.none,
        ),
        labelText: label,
      ),
    );
  }
}
