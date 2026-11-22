import { Component, EventEmitter, Input, Output } from '@angular/core';

/**
 * Ventana de confirmación reutilizable (por ejemplo, antes de eliminar).
 * Uso: <app-confirmar-dialogo [mensaje]="..." (confirmar)="..." (cancelar)="..." />
 */
@Component({
  selector: 'app-confirmar-dialogo',
  standalone: true,
  template: `
    <div class="fixed inset-0 z-40 bg-slate-950/50 flex items-center justify-center p-4">
      <div class="bg-white rounded-2xl border border-slate-200 shadow-xl w-full max-w-md p-6 space-y-4">
        <h2 class="vet-section-title text-lg font-bold">{{ titulo }}</h2>
        <p class="text-sm text-slate-600">{{ mensaje }}</p>
        <div class="flex justify-end gap-2 pt-3 border-t border-slate-100">
          <button
            type="button"
            (click)="cancelar.emit()"
            [disabled]="procesando"
            class="rounded-lg border border-slate-300 text-slate-700 text-sm px-4 py-2 cursor-pointer disabled:opacity-50"
          >
            Cancelar
          </button>
          <button
            type="button"
            (click)="confirmar.emit()"
            [disabled]="procesando"
            class="bg-red-600 hover:bg-red-700 text-white text-sm font-semibold px-5 py-2 rounded-lg cursor-pointer disabled:opacity-50"
          >
            {{ procesando ? 'Procesando...' : textoConfirmar }}
          </button>
        </div>
      </div>
    </div>
  `
})
export class ConfirmarDialogoComponent {
  @Input() titulo = '¿Confirmar acción?';
  @Input() mensaje = '';
  @Input() textoConfirmar = 'Eliminar';
  @Input() procesando = false;
  @Output() confirmar = new EventEmitter<void>();
  @Output() cancelar = new EventEmitter<void>();
}
