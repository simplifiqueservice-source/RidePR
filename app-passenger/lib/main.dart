import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  runApp(const RidePrPassengerApp());
}

class AppConfig {
  static const apiBaseUrl = String.fromEnvironment(
    'RIDEPR_API_URL',
    defaultValue: 'https://localhost:7045',
  );
}

class RidePrPassengerApp extends StatefulWidget {
  const RidePrPassengerApp({super.key});

  @override
  State<RidePrPassengerApp> createState() => _RidePrPassengerAppState();
}

class _RidePrPassengerAppState extends State<RidePrPassengerApp> {
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
      title: 'RidePR Passageiro',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xff0f766e),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
      ),
      home: loading
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : session.isAuthenticated
              ? PassengerHomePage(session: session)
              : LoginPage(session: session, requiredRole: 'Passenger'),
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

  Future<dynamic> post(String path, Map<String, dynamic> body) async {
    return _send(() => http.post(
          _uri(path),
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
                    'App do passageiro',
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
        throw ApiException('Usuario sem perfil de passageiro.');
      }
      if (!mounted) return;
      Navigator.of(context).pushReplacement(
        MaterialPageRoute(
          builder: (_) => PassengerHomePage(session: widget.session),
        ),
      );
    } catch (ex) {
      setState(() => error = ex.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }
}

class PassengerHomePage extends StatefulWidget {
  const PassengerHomePage({required this.session, super.key});

  final AuthSession session;

  @override
  State<PassengerHomePage> createState() => _PassengerHomePageState();
}

class _PassengerHomePageState extends State<PassengerHomePage> {
  Map<String, dynamic>? passenger;
  Map<String, dynamic>? wallet;
  Map<String, dynamic>? lastTrip;
  String? message;
  bool loading = true;

  final origin = TextEditingController(text: 'Centro');
  final destination = TextEditingController(text: 'Aeroporto');
  final originLat = TextEditingController(text: '-25.4284');
  final originLng = TextEditingController(text: '-49.2733');
  final destLat = TextEditingController(text: '-25.5285');
  final destLng = TextEditingController(text: '-49.1758');

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('RidePR Passageiro'),
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
                    title: widget.session.name ?? 'Passageiro',
                    subtitle: widget.session.email ?? '',
                    trailing: wallet == null
                        ? 'Carteira indisponivel'
                        : 'R\$ ${wallet!['balance']}',
                  ),
                  const SizedBox(height: 16),
                  _SectionCard(
                    title: 'Nova corrida',
                    child: Column(
                      children: [
                        _field(origin, 'Origem'),
                        _field(destination, 'Destino'),
                        Row(
                          children: [
                            Expanded(child: _field(originLat, 'Lat. origem')),
                            const SizedBox(width: 12),
                            Expanded(child: _field(originLng, 'Lng. origem')),
                          ],
                        ),
                        Row(
                          children: [
                            Expanded(child: _field(destLat, 'Lat. destino')),
                            const SizedBox(width: 12),
                            Expanded(child: _field(destLng, 'Lng. destino')),
                          ],
                        ),
                        const SizedBox(height: 12),
                        SizedBox(
                          width: double.infinity,
                          child: FilledButton.icon(
                            onPressed: _createTrip,
                            icon: const Icon(Icons.local_taxi),
                            label: const Text('Solicitar corrida'),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  if (lastTrip != null)
                    _SectionCard(
                      title: 'Ultima corrida',
                      child: Text(const JsonEncoder.withIndent('  ')
                          .convert(lastTrip)),
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
      passenger = await widget.session.api.get('/api/passengers/by-user/$userId')
          as Map<String, dynamic>;
      wallet = await widget.session.api.get('/api/payments/wallets/$userId')
          as Map<String, dynamic>;
    } catch (ex) {
      message = ex.toString();
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _createTrip() async {
    if (passenger == null) {
      setState(() => message = 'Cadastro de passageiro nao encontrado.');
      return;
    }

    try {
      final data = await widget.session.api.post('/api/Trips', {
        'passengerId': passenger!['id'],
        'origin': origin.text,
        'destination': destination.text,
        'originLatitude': double.parse(originLat.text),
        'originLongitude': double.parse(originLng.text),
        'destinationLatitude': double.parse(destLat.text),
        'destinationLongitude': double.parse(destLng.text),
      }) as Map<String, dynamic>;

      setState(() {
        lastTrip = data;
        message = 'Corrida solicitada com sucesso.';
      });
    } catch (ex) {
      setState(() => message = ex.toString());
    }
  }

  Future<void> _logout() async {
    await widget.session.logout();
    if (!mounted) return;
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(
        builder: (_) =>
            LoginPage(session: widget.session, requiredRole: 'Passenger'),
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
            const CircleAvatar(child: Icon(Icons.person)),
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
