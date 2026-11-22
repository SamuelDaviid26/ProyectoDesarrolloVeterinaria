import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, firstValueFrom } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { VeterinariaService } from '../../../core/services/veterinaria.service';
import { VeterinarioResponse } from '../../../core/models/api.models';
import { mensajeDeError } from '../../../core/utils/http-error';
import { ConfirmarDialogoComponent } from '../../../shared/confirmar-dialogo/confirmar-dialogo.component';

/**
 * Sección 3.C del enunciado: nombre completo, número de colegiado y fotografía de cada veterinario.
 * Las fotos están protegidas con JWT, por eso se descargan como Blob y se muestran con una URL local.
 */
@Component({
  selector: 'app-veterinarios',
  standalone: true,
  imports: [FormsModule, ConfirmarDialogoComponent],
  templateUrl: './veterinarios.component.html'
})
export class VeterinariosComponent implements OnInit, OnDestroy {
  readonly veterinarios = signal<VeterinarioResponse[]>([]);
  readonly fotos = signal<Record<number, string>>({});
  readonly cargando = signal(false);
  readonly mensajeExito = signal('');
  readonly mensajeError = signal('');

  filtroNombre = '';
  filtroColegiado = '';

  readonly modalAbierto = signal(false);
  readonly errorModal = signal('');
  readonly guardando = signal(false);
  readonly vistaPrevia = signal<string | null>(null);
  editandoId: number | null = null;
  form = { nombreCompleto: '', numeroColegiado: '' };
  fotoSeleccionada: File | null = null;

  readonly porEliminar = signal<VeterinarioResponse | null>(null);
  readonly eliminando = signal(false);

  constructor(
    private readonly adminService: AdminService,
    private readonly vetService: VeterinariaService
  ) {}

  ngOnInit(): void {
    this.buscar();
  }

  ngOnDestroy(): void {
    this.liberarFotos();
    this.liberarVistaPrevia();
  }

  buscar(): void {
    this.cargando.set(true);
    this.mensajeError.set('');
    this.adminService.listarVeterinarios(this.filtroNombre, this.filtroColegiado).subscribe({
      next: (veterinarios) => {
        this.veterinarios.set(veterinarios);
        this.cargando.set(false);
        void this.cargarFotos(veterinarios);
      },
      error: (err) => {
        this.mensajeError.set(mensajeDeError(err, 'No se pudieron cargar los veterinarios.'));
        this.cargando.set(false);
      }
    });
  }

  limpiarFiltros(): void {
    this.filtroNombre = '';
    this.filtroColegiado = '';
    this.buscar();
  }

  abrirNuevo(): void {
    this.editandoId = null;
    this.form = { nombreCompleto: '', numeroColegiado: '' };
    this.abrirModal();
  }

  abrirEditar(vet: VeterinarioResponse): void {
    this.editandoId = vet.id;
    this.form = { nombreCompleto: vet.nombreCompleto, numeroColegiado: vet.numeroColegiado };
    this.abrirModal();
    // Mientras no elija otra, se muestra la foto actual
    this.vistaPrevia.set(this.fotos()[vet.id] ?? null);
  }

  onFotoSeleccionada(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.fotoSeleccionada = input.files && input.files.length > 0 ? input.files[0] : null;
    this.liberarVistaPrevia();
    this.vistaPrevia.set(this.fotoSeleccionada ? URL.createObjectURL(this.fotoSeleccionada) : null);
  }

  guardar(): void {
    const payload = {
      nombreCompleto: this.form.nombreCompleto.trim(),
      numeroColegiado: this.form.numeroColegiado.trim(),
      foto: this.fotoSeleccionada
    };
    if (!payload.nombreCompleto || !payload.numeroColegiado) {
      this.errorModal.set('El nombre completo y el número de colegiado son obligatorios.');
      return;
    }
    if (!this.editandoId && !payload.foto) {
      this.errorModal.set('La fotografía es obligatoria al registrar un veterinario.');
      return;
    }

    this.guardando.set(true);
    const peticion: Observable<unknown> = this.editandoId
      ? this.adminService.actualizarVeterinario(this.editandoId, payload)
      : this.adminService.crearVeterinario(payload);

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.cerrarModal();
        this.mensajeExito.set(this.editandoId ? 'Veterinario actualizado.' : 'Veterinario registrado.');
        this.buscar();
      },
      error: (err) => {
        this.guardando.set(false);
        this.errorModal.set(mensajeDeError(err, 'No se pudo guardar el veterinario.'));
      }
    });
  }

  cerrarModal(): void {
    this.modalAbierto.set(false);
    this.fotoSeleccionada = null;
    this.liberarVistaPrevia();
  }

  pedirEliminar(vet: VeterinarioResponse): void {
    this.limpiarMensajes();
    this.porEliminar.set(vet);
  }

  confirmarEliminar(): void {
    const vet = this.porEliminar();
    if (!vet) return;

    this.eliminando.set(true);
    this.adminService.eliminarVeterinario(vet.id).subscribe({
      next: () => {
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeExito.set(`${vet.nombreCompleto} fue eliminado.`);
        this.buscar();
      },
      error: (err) => {
        // 409 si está vinculado a una cuenta de usuario o al expediente clínico
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeError.set(mensajeDeError(err, 'No se pudo eliminar el veterinario.'));
      }
    });
  }

  private abrirModal(): void {
    this.limpiarMensajes();
    this.errorModal.set('');
    this.fotoSeleccionada = null;
    this.liberarVistaPrevia();
    this.modalAbierto.set(true);
  }

  private async cargarFotos(veterinarios: VeterinarioResponse[]): Promise<void> {
    this.liberarFotos();
    const fotos: Record<number, string> = {};
    for (const vet of veterinarios) {
      if (!vet.fotoUrl) continue;
      try {
        const blob = await firstValueFrom(this.vetService.obtenerArchivoBlob(vet.fotoUrl));
        fotos[vet.id] = URL.createObjectURL(blob);
      } catch {
        // Si una foto falla se muestra "Sin foto" y se continúa con las demás
      }
    }
    this.fotos.set(fotos);
  }

  private liberarFotos(): void {
    for (const url of Object.values(this.fotos())) {
      URL.revokeObjectURL(url);
    }
    this.fotos.set({});
  }

  /** Solo se libera la vista previa si es un archivo nuevo (no la foto que ya está en la lista). */
  private liberarVistaPrevia(): void {
    const url = this.vistaPrevia();
    if (url && !Object.values(this.fotos()).includes(url)) {
      URL.revokeObjectURL(url);
    }
    this.vistaPrevia.set(null);
  }

  private limpiarMensajes(): void {
    this.mensajeExito.set('');
    this.mensajeError.set('');
  }
}
