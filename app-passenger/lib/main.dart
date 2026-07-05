import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_map/flutter_map.dart';
import 'package:http/http.dart' as http;
import 'package:latlong2/latlong.dart';
import 'package:signalr_netcore/signalr_client.dart';

const defaultApiBaseUrl = 'http://45.185.199.173:8282';

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
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xff2563eb)),
        useMaterial3: true,
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
  final baseUrlController = TextEditingController(text: defaultApiBaseUrl);
  final emailController =
      TextEditingController(text: 'passageiro.mvp@ridepr.test');
  final passwordController = TextEditingController(text: 'Senha123!');
  final passengerIdController = TextEditingController();
  final passengerCpfController = TextEditingController(text: '11122233344');
  final passengerBirthDateController =
      TextEditingController(text: '1990-01-01');
  final passengerPhoneController = TextEditingController(text: '11999990000');
  final passengerEmergencyPhoneController =
      TextEditingController(text: '11999990001');
  final passengerAddressController = TextEditingController(text: 'Rua MVP');
  final passengerCityController = TextEditingController(text: 'Sao Paulo');
  final passengerStateController = TextEditingController(text: 'SP');
  final passengerZipCodeController = TextEditingController(text: '01001000');
  final driverIdController =
      TextEditingController(text: 'd4ff8255-d3fe-4fb6-9fb9-2daaae8398c1');
  final tripIdController = TextEditingController();
  final originController =
      TextEditingController(text: 'Praca da Se, Sao Paulo');
  final destinationController =
      TextEditingController(text: 'Avenida Paulista, Sao Paulo');
  final originLatController = TextEditingController(text: '-23.55052');
  final originLngController = TextEditingController(text: '-46.63331');
  final destinationLatController = TextEditingController(text: '-23.56141');
  final destinationLngController = TextEditingController(text: '-46.65588');
  final radiusController = TextEditingController(text: '5');
  final actualDistanceController = TextEditingController(text: '4.2');
  final actualDurationController = TextEditingController(text: '18');

  late final ApiClient api = ApiClient(baseUrl: baseUrlController.text);
  final mapController = MapController();
  HubConnection? hubConnection;
  int tabIndex = 0;
  bool loading = false;
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

  bool get loggedIn => accessToken != null && accessToken!.isNotEmpty;
  bool get hasTripId => tripIdController.text.trim().isNotEmpty;
  bool get hasDriverId => driverIdController.text.trim().isNotEmpty;

  @override
  void initState() {
    super.initState();
    tripIdController.addListener(_refreshActionState);
    driverIdController.addListener(_refreshActionState);
  }

  @override
  void dispose() {
    hubConnection?.stop();
    tripIdController.removeListener(_refreshActionState);
    driverIdController.removeListener(_refreshActionState);
    baseUrlController.dispose();
    emailController.dispose();
    passwordController.dispose();
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
    return Scaffold(
      appBar: AppBar(
        title: const Text('RidePR Passageiro'),
        actions: [
          TextButton.icon(
            onPressed: () => setState(() => debugVisible = !debugVisible),
            icon: const Icon(Icons.bug_report),
            label: const Text('Debug'),
          ),
          IconButton(
            tooltip: 'Limpar token',
            onPressed: _clearSession,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: SafeArea(
        child: IndexedStack(
          index: tabIndex,
          children: [
            _LoginScreen(
              baseUrlController: baseUrlController,
              emailController: emailController,
              passwordController: passwordController,
              loggedIn: loggedIn,
              loading: loading,
              liveStatus: liveStatus,
              onLogin: _login,
            ),
            _CreateTripScreen(
              passengerCpfController: passengerCpfController,
              passengerBirthDateController: passengerBirthDateController,
              passengerPhoneController: passengerPhoneController,
              passengerEmergencyPhoneController:
                  passengerEmergencyPhoneController,
              passengerAddressController: passengerAddressController,
              passengerCityController: passengerCityController,
              passengerStateController: passengerStateController,
              passengerZipCodeController: passengerZipCodeController,
              passengerIdController: passengerIdController,
              tripIdController: tripIdController,
              originController: originController,
              destinationController: destinationController,
              originLatController: originLatController,
              originLngController: originLngController,
              destinationLatController: destinationLatController,
              destinationLngController: destinationLngController,
              loading: loading,
              canCreateTrip: loggedIn,
              passengerLoaded: passenger != null,
              onSavePassenger: _savePassenger,
              onCreateTrip: _createTrip,
            ),
            _StatusScreen(
              tripIdController: tripIdController,
              tripStatus: tripStatus,
              liveStatus: liveStatus,
              originPoint: originPoint,
              destinationPoint: destinationPoint,
              driverPoint: driverPoint,
              mapController: mapController,
              loading: loading,
              onRefresh: _getTrip,
            ),
            _TestButtonsScreen(
              tripIdController: tripIdController,
              driverIdController: driverIdController,
              radiusController: radiusController,
              actualDistanceController: actualDistanceController,
              actualDurationController: actualDurationController,
              loading: loading,
              canRequestDispatch: loggedIn && hasTripId,
              canDriverAction: loggedIn && hasTripId && hasDriverId,
              onRequestDispatch: _requestDispatch,
              onAccept: _acceptTrip,
              onStart: _startTrip,
              onFinish: _finishTrip,
            ),
          ],
        ),
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: tabIndex,
        onDestinationSelected: (value) => setState(() => tabIndex = value),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.login), label: 'Login'),
          NavigationDestination(icon: Icon(Icons.add_road), label: 'Solicitar'),
          NavigationDestination(icon: Icon(Icons.map), label: 'Status'),
          NavigationDestination(icon: Icon(Icons.tune), label: 'Testes'),
        ],
      ),
      bottomSheet: debugVisible ? _ResponsePanel(response: lastResponse) : null,
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
        await _loadPassenger();
        await _connectRealtime();
        tabIndex = 1;
      }

      return result;
    });
  }

  Future<void> _createTrip() async {
    await _run(() async {
      _requireLoggedIn();
      _requirePassenger();
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
        tabIndex = 2;
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

  Future<void> _requestDispatch() async {
    await _run(() {
      _requireLoggedIn();
      _requireTripId();
      return api.post('/api/dispatch/request', {
        'tripId': tripIdController.text.trim(),
        'radiusKm': _doubleValue(radiusController),
        'timeoutSeconds': 30,
        'maxCandidates': 10,
      });
    });
  }

  Future<void> _acceptTrip() async {
    await _run(() {
      _requireLoggedIn();
      _requireTripId();
      _requireDriverId();
      return api.post('/api/dispatch/accept', {
        'tripId': tripIdController.text.trim(),
        'driverId': driverIdController.text.trim(),
      });
    });
  }

  Future<void> _startTrip() async {
    await _run(() {
      _requireLoggedIn();
      _requireTripId();
      _requireDriverId();
      return api.post('/api/trips/${tripIdController.text.trim()}/start', {
        'driverId': driverIdController.text.trim(),
      });
    });
  }

  Future<void> _finishTrip() async {
    await _run(() {
      _requireLoggedIn();
      _requireTripId();
      _requireDriverId();
      return api.post('/api/trips/${tripIdController.text.trim()}/finish', {
        'driverId': driverIdController.text.trim(),
        'actualDistanceKm': _doubleValue(actualDistanceController),
        'actualDurationMinutes': _doubleValue(actualDurationController),
      });
    });
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
    await hubConnection?.stop();

    final hubUrl =
        '${baseUrlController.text.trim().replaceAll(RegExp(r'/+$'), '')}/driverHub';
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

      setState(() {
        driverPoint = LatLng(
          _numField(location, 'latitude'),
          _numField(location, 'longitude'),
        );
        liveStatus = 'Localizacao do motorista atualizada';
        lastResponse = _prettyRealtime('DriverLocationUpdated', location);
      });
      _moveMapToVisiblePoint();
    });

    await connection.start();

    if (mounted) {
      setState(() {
        hubConnection = connection;
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

    if (point != null) {
      mapController.move(point, 14);
    }
  }

  void _clearSession() {
    hubConnection?.stop();
    setState(() {
      accessToken = null;
      api.accessToken = null;
      hubConnection = null;
      liveStatus = 'SignalR desconectado.';
      lastResponse = 'Token removido da memoria.';
    });
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

  void _requireDriverId() {
    if (!hasDriverId) {
      throw LocalValidationException('Informe o DriverId do motorista.');
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
      'TripRequested' => 'Corrida solicitada',
      'TripAccepted' => 'Corrida aceita',
      'TripStarted' => 'Corrida iniciada',
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

class _LoginScreen extends StatelessWidget {
  const _LoginScreen({
    required this.baseUrlController,
    required this.emailController,
    required this.passwordController,
    required this.loggedIn,
    required this.loading,
    required this.liveStatus,
    required this.onLogin,
  });

  final TextEditingController baseUrlController;
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final bool loggedIn;
  final bool loading;
  final String liveStatus;
  final VoidCallback onLogin;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Entrar no RidePR',
      children: [
        _Input(controller: baseUrlController, label: 'Base URL da API'),
        _Input(controller: emailController, label: 'E-mail'),
        _Input(
          controller: passwordController,
          label: 'Senha',
          obscureText: true,
        ),
        FilledButton.icon(
          onPressed: loading ? null : onLogin,
          icon: const Icon(Icons.login),
          label: Text(loggedIn ? 'Login OK - entrar novamente' : 'Entrar'),
        ),
        _StatusPill(
          text: loggedIn
              ? 'Conectado. Voce ja pode pedir corrida.'
              : 'Entre para pedir uma corrida.',
          icon: loggedIn ? Icons.check_circle : Icons.info,
        ),
        _StatusPill(text: liveStatus, icon: Icons.wifi_tethering),
      ],
    );
  }
}

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
            _Input(controller: passengerIdController, label: 'PassengerId'),
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
        _Input(controller: tripIdController, label: 'Codigo da corrida'),
        _Input(controller: originController, label: 'Origem'),
        _Input(controller: destinationController, label: 'Destino'),
        ExpansionTile(
          title: const Text('Coordenadas'),
          children: [
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

class _StatusScreen extends StatelessWidget {
  const _StatusScreen({
    required this.tripIdController,
    required this.tripStatus,
    required this.liveStatus,
    required this.originPoint,
    required this.destinationPoint,
    required this.driverPoint,
    required this.mapController,
    required this.loading,
    required this.onRefresh,
  });

  final TextEditingController tripIdController;
  final String tripStatus;
  final String liveStatus;
  final LatLng? originPoint;
  final LatLng? destinationPoint;
  final LatLng? driverPoint;
  final MapController mapController;
  final bool loading;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Sua corrida',
      children: [
        Text(
          tripStatus,
          style: Theme.of(context).textTheme.titleLarge,
        ),
        _StatusPill(text: liveStatus, icon: Icons.wifi_tethering),
        SizedBox(
          height: 360,
          child: ClipRRect(
            borderRadius: BorderRadius.circular(8),
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
                MarkerLayer(
                  markers: [
                    if (originPoint != null)
                      _mapMarker(originPoint!, Icons.trip_origin, Colors.green),
                    if (destinationPoint != null)
                      _mapMarker(destinationPoint!, Icons.flag, Colors.red),
                    if (driverPoint != null)
                      _mapMarker(driverPoint!, Icons.local_taxi, Colors.blue),
                  ],
                ),
              ],
            ),
          ),
        ),
        _Input(controller: tripIdController, label: 'TripId'),
        FilledButton.icon(
          onPressed: loading ? null : onRefresh,
          icon: const Icon(Icons.refresh),
          label: const Text('Consultar corrida'),
        ),
      ],
    );
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
}

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

class _StatusPill extends StatelessWidget {
  const _StatusPill({required this.text, required this.icon});

  final String text;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Row(
          children: [
            Icon(icon, size: 20),
            const SizedBox(width: 8),
            Expanded(child: Text(text)),
          ],
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
    return TextField(
      controller: controller,
      obscureText: obscureText,
      decoration: InputDecoration(
        border: const OutlineInputBorder(),
        labelText: label,
      ),
    );
  }
}

class _ResponsePanel extends StatelessWidget {
  const _ResponsePanel({required this.response});

  final String response;

  @override
  Widget build(BuildContext context) {
    return Material(
      elevation: 12,
      child: SizedBox(
        height: 168,
        width: double.infinity,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: DecoratedBox(
            decoration: BoxDecoration(
              border: Border.all(color: Theme.of(context).dividerColor),
              borderRadius: BorderRadius.circular(8),
            ),
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(10),
              child: SelectableText(
                response,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      fontFamily: 'monospace',
                    ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
