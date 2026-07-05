import 'package:flutter_test/flutter_test.dart';

import 'package:ridepr_passenger/main.dart';

void main() {
  testWidgets('opens passenger app', (WidgetTester tester) async {
    await tester.pumpWidget(const RidePrMvpTestApp());

    expect(find.text('RidePR Passageiro'), findsOneWidget);
    expect(find.text('Entrar no RidePR'), findsOneWidget);
  });
}
