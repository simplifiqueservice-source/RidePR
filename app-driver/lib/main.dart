import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  runApp(const RidePrDriverApp());
}

class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'RIDEPR_API_URL',
    defaultValue: 'https://localhost:7045',
  );
}

class RidePrDriverApp extends StatefulWidget {
  const RidePrDriverApp({super.key});

  @override
  State<RidePrDriverApp> createState() => _RidePrDriverAppState();
}

class _RidePrDriverAppState extends State<RidePrDriverApp> {
  final session = AuthSession(ApiClient(AppConfig.apiBaseUrl));
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
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xff1d4ed8),
          brightness: Brightness.light,
        ),
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

  final String baseUrl;
  String? accessToken;

  Uri _uri(String path, [Map<String, dynamic>? query]) {
    final normalized = path.startsWith('/') ? path : '/$path';
    final values = query?.map((key, value) => MapEntry(key, '$value'));
    return Uri.parse('$baseUrl$normalized').replace(queryParameters: values);
  }

  Future<dynamic> get(String path, [Map<String, dynamic>? query]) async {
    return _send(() => http.get(_uri(path, query), headers: _headers()));
  }

  Future<dynamic> post(String path, Map<String, dynamic> body,
      [Map<String, dynamic>? query]) async {
    return _send(() => http.post(
          _uri(path, query),
          headers: _headers(),
          body: jsonEncode(body),
        ));
  }

  Future<dynamic> patch(String path, Map<String, dynamic> body) async {
    return _send(() => http.patch(
          _uri(path),
          headers: _headers(),
          body: jsonEncode(body),
        ));
  }

  Map<String, String> _headers() {
    return {
      'Content-Type': 'application/json',
      if (accessToken != null) 'Authorization': 'Bearer $accessToken',
    };
  }

  Future<dynamic> _send(Future<http.Response> Function() request) async {
    final response = await request();
    final text = response.body;
    final data = text.isEmpty ? null : jsonDecode(text);

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

  bool get isAuthenticated => api.accessToken != null;

  Future<void> restore() async {
    final prefs = await SharedPreferences.getInstance();
    api.accessToken = prefs.getString('accessToken');
    refreshToken = prefs.getString('refreshToken');
    userId = prefs.getString('userId');
    name = prefs.getString('name');
    email = prefs.getString('email');
    role = prefs.getString('role');
  }

  Future<void> login(String email, String password) async {
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
  final email = TextEditingController();
  final password = TextEditingController();
  bool loading = false;
  String? error;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'RidePR',
                    style: Theme.of(context).textTheme.displaySmall?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    'App do motorista',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 32),
                  TextField(
                    controller: email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: 'E-mail'),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: password,
                    obscureText: true,
                    decoration: const InputDecoration(labelText: 'Senha'),
                  ),
                  if (error != null) ...[
                    const SizedBox(height: 12),
                    Text(error!, style: const TextStyle(color: Colors.red)),
                  ],
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: loading ? null : _submit,
                    child: Text(loading ? 'Entrando...' : 'Entrar'),
                  ),
                ],
              ),
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
      await widget.session.login(email.text.trim(), password.text);
      if (widget.session.role != widget.requiredRole &&
          widget.session.role != 'Administrator') {
        throw ApiException('Usuario sem perfil de motorista.');
      }
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(builder: (_) => DriverHomePage(session: widget.session)),
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
  Map<String, dynamic>? driver;
  String? message;
  bool loading = true;
  int selectedStatus = 2;

  final latitude = TextEditingController(text: '-25.4284');
  final longitude = TextEditingController(text: '-49.2733');
  final speed = TextEditingController(text: '0');
  final heading = TextEditingController(text: '0');
  final tripId = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
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
                padding: const EdgeInsets.all(16),
                children: [
                  _HeaderCard(
                    title: widget.session.name ?? 'Motorista',
                    subtitle: driver == null
                        ? widget.session.email ?? ''
                        : '${driver!['approvalStatus']} | ${driver!['status']}',
                    trailing: driver == null ? 'Sem cadastro' : driver!['phone'],
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: 'Disponibilidade',
                    child: Column(
                      children: [
                        DropdownButtonFormField<int>(
                          value: selectedStatus,
                          decoration: const InputDecoration(labelText: 'Status'),
                          items: const [
                            DropdownMenuItem(value: 1, child: Text('Offline')),
                            DropdownMenuItem(value: 2, child: Text('Online')),
                            DropdownMenuItem(value: 3, child: Text('Ocupado')),
                            DropdownMenuItem(value: 4, child: Text('Pausado')),
                          ],
                          onChanged: (value) =>
                              setState(() => selectedStatus = value ?? 2),
                        ),
                        const SizedBox(height: 12),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton.icon(
                            onPressed: _updateStatus,
                            icon: const Icon(Icons.power_settings_new),
                            label: const Text('Atualizar status'),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: 'Localizacao',
                    child: Column(
                      children: [
                        Row(
                          children: [
                            Expanded(child: _field(latitude, 'Latitude')),
                            const SizedBox(width: 12),
                            Expanded(child: _field(longitude, 'Longitude')),
                          ],
                        ),
                        Row(
                          children: [
                            Expanded(child: _field(speed, 'Velocidade')),
                            const SizedBox(width: 12),
                            Expanded(child: _field(heading, 'Direcao')),
                          ],
                        ),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton.icon(
                            onPressed: _updateLocation,
                            icon: const Icon(Icons.my_location),
                            label: const Text('Enviar localizacao'),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: 'Despacho',
                    child: Column(
                      children: [
                        _field(tripId, 'ID da corrida ofertada'),
                        Row(
                          children: [
                            Expanded(
                              child: FilledButton.icon(
                                onPressed: () => _dispatchDecision(true),
                                icon: const Icon(Icons.check),
                                label: const Text('Aceitar'),
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: OutlinedButton.icon(
                                onPressed: () => _dispatchDecision(false),
                                icon: const Icon(Icons.close),
                                label: const Text('Recusar'),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ),
                  if (message != null) ...[
                    const SizedBox(height: 16),
                    Text(message!),
                  ],
                ],
              ),
            ),
    );
  }

  Widget _field(TextEditingController controller, String label) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: TextField(
        controller: controller,
        decoration: InputDecoration(labelText: label),
      ),
    );
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      message = null;
    });

    try {
      final userId = widget.session.userId!;
      driver = await widget.session.api.get('/api/drivers/by-user/$userId')
          as Map<String, dynamic>;
      selectedStatus = _statusToNumber(driver!['status'] as String?);
    } catch (ex) {
      message = ex.toString();
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _updateStatus() async {
    if (driver == null) return;

    try {
      final data = await widget.session.api.patch(
        '/api/drivers/${driver!['id']}/status',
        {'status': selectedStatus},
      ) as Map<String, dynamic>;
      setState(() {
        driver = data;
        message = 'Status atualizado.';
      });
    } catch (ex) {
      setState(() => message = ex.toString());
    }
  }

  Future<void> _updateLocation() async {
    if (driver == null) return;

    try {
      await widget.session.api.post(
        '/api/driver-location',
        {},
        {
          'driverId': driver!['id'],
          'latitude': double.parse(latitude.text),
          'longitude': double.parse(longitude.text),
          'speed': double.parse(speed.text),
          'heading': double.parse(heading.text),
        },
      );
      setState(() => message = 'Localizacao enviada.');
    } catch (ex) {
      setState(() => message = ex.toString());
    }
  }

  Future<void> _dispatchDecision(bool accept) async {
    if (driver == null || tripId.text.trim().isEmpty) return;

    try {
      final path = accept ? 'accept' : 'reject';
      final data = await widget.session.api.post(
        '/api/dispatch/${tripId.text.trim()}/$path',
        {'driverId': driver!['id']},
      );
      setState(() {
        message = const JsonEncoder.withIndent('  ').convert(data);
      });
    } catch (ex) {
      setState(() => message = ex.toString());
    }
  }

  int _statusToNumber(String? status) {
    return switch (status) {
      'Offline' => 1,
      'Online' => 2,
      'Busy' => 3,
      'Paused' => 4,
      _ => 2,
    };
  }

  Future<void> _logout() async {
    await widget.session.logout();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) => LoginPage(session: widget.session, requiredRole: 'Driver'),
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
            Text(trailing, style: Theme.of(context).textTheme.titleMedium),
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
