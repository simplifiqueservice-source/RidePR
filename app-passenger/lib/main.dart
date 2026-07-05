import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

void main() {
  runApp(const RidePrMvpTestApp());
}

class RidePrMvpTestApp extends StatelessWidget {
  const RidePrMvpTestApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'RidePR MVP Test',
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

class MvpTestHome extends StatefulWidget {
  const MvpTestHome({super.key});

  @override
  State<MvpTestHome> createState() => _MvpTestHomeState();
}

class _MvpTestHomeState extends State<MvpTestHome> {
  final baseUrlController =
      TextEditingController(text: 'http://192.168.0.10:5000');
  final emailController =
      TextEditingController(text: 'passageiro.mvp@ridepr.test');
  final passwordController = TextEditingController(text: 'Senha123!');
  final passengerIdController = TextEditingController();
  final driverIdController = TextEditingController();
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
  int tabIndex = 0;
  bool loading = false;
  String? accessToken;
  String lastResponse = 'Nenhuma chamada executada ainda.';

  bool get loggedIn => accessToken != null && accessToken!.isNotEmpty;

  @override
  void dispose() {
    baseUrlController.dispose();
    emailController.dispose();
    passwordController.dispose();
    passengerIdController.dispose();
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('RidePR MVP Test Client'),
        actions: [
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
              onLogin: _login,
            ),
            _CreateTripScreen(
              passengerIdController: passengerIdController,
              tripIdController: tripIdController,
              originController: originController,
              destinationController: destinationController,
              originLatController: originLatController,
              originLngController: originLngController,
              destinationLatController: destinationLatController,
              destinationLngController: destinationLngController,
              loading: loading,
              onCreateTrip: _createTrip,
            ),
            _StatusScreen(
              tripIdController: tripIdController,
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
          NavigationDestination(icon: Icon(Icons.route), label: 'Status'),
          NavigationDestination(icon: Icon(Icons.tune), label: 'Testes'),
        ],
      ),
      bottomSheet: _ResponsePanel(response: lastResponse),
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

      if (result.success && token != null) {
        accessToken = token;
        api.accessToken = token;
      }

      return result;
    });
  }

  Future<void> _createTrip() async {
    await _run(() async {
      final result = await api.post('/api/trips', {
        'passengerId': passengerIdController.text.trim(),
        'origin': originController.text.trim(),
        'destination': destinationController.text.trim(),
        'originLatitude': _doubleValue(originLatController),
        'originLongitude': _doubleValue(originLngController),
        'destinationLatitude': _doubleValue(destinationLatController),
        'destinationLongitude': _doubleValue(destinationLngController),
      });

      final tripId = result.body is Map<String, dynamic>
          ? result.body['id'] as String?
          : null;

      if (result.success && tripId != null) {
        tripIdController.text = tripId;
      }

      return result;
    });
  }

  Future<void> _requestDispatch() async {
    await _run(() {
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
      return api.post('/api/dispatch/accept', {
        'tripId': tripIdController.text.trim(),
        'driverId': driverIdController.text.trim(),
      });
    });
  }

  Future<void> _startTrip() async {
    await _run(() {
      return api.post('/api/trips/${tripIdController.text.trim()}/start', {
        'driverId': driverIdController.text.trim(),
      });
    });
  }

  Future<void> _finishTrip() async {
    await _run(() {
      return api.post('/api/trips/${tripIdController.text.trim()}/finish', {
        'driverId': driverIdController.text.trim(),
        'actualDistanceKm': _doubleValue(actualDistanceController),
        'actualDurationMinutes': _doubleValue(actualDurationController),
      });
    });
  }

  Future<void> _getTrip() async {
    await _run(() => api.get('/api/trips/${tripIdController.text.trim()}'));
  }

  Future<void> _run(Future<ApiResult> Function() action) async {
    setState(() => loading = true);

    try {
      final result = await action();

      setState(() => lastResponse = result.pretty());
    } catch (error) {
      setState(() => lastResponse = 'Erro local\n$error');
    } finally {
      if (mounted) {
        setState(() => loading = false);
      }
    }
  }

  void _clearSession() {
    setState(() {
      accessToken = null;
      api.accessToken = null;
      lastResponse = 'Token removido da memoria.';
    });
  }

  static double _doubleValue(TextEditingController controller) {
    return double.tryParse(controller.text.trim().replaceAll(',', '.')) ?? 0;
  }
}

class _LoginScreen extends StatelessWidget {
  const _LoginScreen({
    required this.baseUrlController,
    required this.emailController,
    required this.passwordController,
    required this.loggedIn,
    required this.loading,
    required this.onLogin,
  });

  final TextEditingController baseUrlController;
  final TextEditingController emailController;
  final TextEditingController passwordController;
  final bool loggedIn;
  final bool loading;
  final VoidCallback onLogin;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Login',
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
        Text(
          loggedIn
              ? 'Token salvo em memoria.'
              : 'Informe o IP/porta do servidor e faca login.',
        ),
      ],
    );
  }
}

class _CreateTripScreen extends StatelessWidget {
  const _CreateTripScreen({
    required this.passengerIdController,
    required this.tripIdController,
    required this.originController,
    required this.destinationController,
    required this.originLatController,
    required this.originLngController,
    required this.destinationLatController,
    required this.destinationLngController,
    required this.loading,
    required this.onCreateTrip,
  });

  final TextEditingController passengerIdController;
  final TextEditingController tripIdController;
  final TextEditingController originController;
  final TextEditingController destinationController;
  final TextEditingController originLatController;
  final TextEditingController originLngController;
  final TextEditingController destinationLatController;
  final TextEditingController destinationLngController;
  final bool loading;
  final VoidCallback onCreateTrip;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Solicitar corrida',
      children: [
        _Input(controller: passengerIdController, label: 'PassengerId'),
        _Input(controller: tripIdController, label: 'TripId gerado/manual'),
        _Input(controller: originController, label: 'Origem'),
        _Input(controller: destinationController, label: 'Destino'),
        Row(
          children: [
            Expanded(
                child: _Input(
                    controller: originLatController, label: 'Lat origem')),
            const SizedBox(width: 8),
            Expanded(
                child: _Input(
                    controller: originLngController, label: 'Lng origem')),
          ],
        ),
        Row(
          children: [
            Expanded(
                child: _Input(
                    controller: destinationLatController,
                    label: 'Lat destino')),
            const SizedBox(width: 8),
            Expanded(
                child: _Input(
                    controller: destinationLngController,
                    label: 'Lng destino')),
          ],
        ),
        FilledButton.icon(
          onPressed: loading ? null : onCreateTrip,
          icon: const Icon(Icons.local_taxi),
          label: const Text('Criar corrida'),
        ),
      ],
    );
  }
}

class _StatusScreen extends StatelessWidget {
  const _StatusScreen({
    required this.tripIdController,
    required this.loading,
    required this.onRefresh,
  });

  final TextEditingController tripIdController;
  final bool loading;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    return _ScreenFrame(
      title: 'Acompanhar status',
      children: [
        _Input(controller: tripIdController, label: 'TripId'),
        FilledButton.icon(
          onPressed: loading ? null : onRefresh,
          icon: const Icon(Icons.refresh),
          label: const Text('Consultar corrida'),
        ),
      ],
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
            controller: actualDistanceController, label: 'Distancia final km'),
        _Input(
            controller: actualDurationController,
            label: 'Duracao final minutos'),
        FilledButton.icon(
          onPressed: loading ? null : onRequestDispatch,
          icon: const Icon(Icons.radar),
          label: const Text('Solicitar dispatch'),
        ),
        FilledButton.icon(
          onPressed: loading ? null : onAccept,
          icon: const Icon(Icons.check_circle),
          label: const Text('Aceitar corrida'),
        ),
        FilledButton.icon(
          onPressed: loading ? null : onStart,
          icon: const Icon(Icons.play_arrow),
          label: const Text('Iniciar corrida'),
        ),
        FilledButton.icon(
          onPressed: loading ? null : onFinish,
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
