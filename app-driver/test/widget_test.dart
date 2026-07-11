import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:ridepr_driver/main.dart';

void main() {
  testWidgets('opens driver app', (WidgetTester tester) async {
    SharedPreferences.setMockInitialValues({});

    await tester.pumpWidget(const RidePrDriverApp());
    await tester.pumpAndSettle();

    expect(find.text('RidePR'), findsOneWidget);
    expect(find.text('Dirija com um app simples e profissional'), findsOneWidget);
    expect(find.text('Entrar'), findsOneWidget);
    expect(find.text('Criar conta'), findsOneWidget);
  });
}
