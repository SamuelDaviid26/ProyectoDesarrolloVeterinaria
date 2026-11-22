import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:veterinaria_movil/screens/bienvenida_screen.dart';
import 'package:veterinaria_movil/tema.dart';

void main() {
  for (final tamano in const [Size(360, 740), Size(1280, 800), Size(800, 1280)]) {
    testWidgets('Bienvenida sin desbordes en $tamano', (tester) async {
      tester.view.physicalSize = tamano;
      tester.view.devicePixelRatio = 1;
      addTearDown(tester.view.reset);
      await tester.pumpWidget(MaterialApp(
          theme: temaVeterinaria(), home: const BienvenidaScreen()));
      await tester.pump();
      expect(find.text('Iniciar sesión'), findsOneWidget);
      expect(find.text('Crear cuenta'), findsOneWidget);
      expect(tester.takeException(), isNull);
    });
  }
}
