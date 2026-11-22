import { Component, OnInit, WritableSignal, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, forkJoin } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { EspecieResponse, RazaResponse } from '../../../core/models/api.models';
import { mensajeDeError } from '../../../core/utils/http-error';
import { ConfirmarDialogoComponent } from '../../../shared/confirmar-dialogo/confirmar-dialogo.component';

interface PendienteEliminar {
  tipo: 'especie' | 'raza';
  id: number;
  nombre: string;
}

/**
 * Sección 3.A del enunciado: buscar, agregar, modificar y eliminar especies y sus razas.
 * Se cargan todas las especies y razas una vez y la búsqueda se filtra en el navegador.
 */
@Component({
  selector: 'app-especies-razas',
  standalone: true,
  imports: [FormsModule, ConfirmarDialogoComponent],
  templateUrl: './especies-razas.component.html'
})
export class EspeciesRazasComponent implements OnInit {
  // Datos
  readonly especies = signal<EspecieResponse[]>([]);
  readonly razas = signal<RazaResponse[]>([]);
  readonly especieSeleccionadaId = signal<number | null>(null);

  // Búsqueda
  readonly filtroEspecie = signal('');
  readonly filtroRaza = signal('');

  readonly especiesFiltradas = computed(() => {
    const texto = this.filtroEspecie().trim().toLowerCase();
    return this.especies().filter((e) => e.nombre.toLowerCase().includes(texto));
  });

  readonly razasFiltradas = computed(() => {
    const texto = this.filtroRaza().trim().toLowerCase();
    const especieId = this.especieSeleccionadaId();
    return this.razas().filter(
      (r) => (especieId === null || r.especieId === especieId) && r.nombre.toLowerCase().includes(texto)
    );
  });

  readonly especieSeleccionada = computed(
    () => this.especies().find((e) => e.id === this.especieSeleccionadaId()) ?? null
  );

  // Estado de la pantalla
  readonly cargando = signal(false);
  readonly mensajeExito = signal('');
  readonly mensajeError = signal('');

  // Modal de especie
  readonly modalEspecie = signal(false);
  editandoEspecieId: number | null = null;
  formEspecie = { nombre: '' };

  // Modal de raza
  readonly modalRaza = signal(false);
  editandoRazaId: number | null = null;
  formRaza = { nombre: '', especieId: 0 };

  readonly errorModal = signal('');
  readonly guardando = signal(false);

  // Confirmación de eliminación
  readonly porEliminar = signal<PendienteEliminar | null>(null);
  readonly eliminando = signal(false);

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.cargarDatos();
  }

  cargarDatos(): void {
    this.cargando.set(true);
    forkJoin({
      especies: this.adminService.listarEspecies(),
      razas: this.adminService.listarRazas()
    }).subscribe({
      next: ({ especies, razas }) => {
        this.especies.set(especies);
        this.razas.set(razas);
        // Si la especie seleccionada ya no existe, se quita la selección
        if (!especies.some((e) => e.id === this.especieSeleccionadaId())) {
          this.especieSeleccionadaId.set(null);
        }
        this.cargando.set(false);
      },
      error: (err) => {
        this.mensajeError.set(mensajeDeError(err, 'No se pudieron cargar las especies y razas.'));
        this.cargando.set(false);
      }
    });
  }

  cantidadRazas(especieId: number): number {
    return this.razas().filter((r) => r.especieId === especieId).length;
  }

  seleccionarEspecie(id: number | null): void {
    this.especieSeleccionadaId.set(this.especieSeleccionadaId() === id ? null : id);
  }

  // ---------- Especies ----------

  abrirNuevaEspecie(): void {
    this.editandoEspecieId = null;
    this.formEspecie = { nombre: '' };
    this.abrirModal(this.modalEspecie);
  }

  abrirEditarEspecie(especie: EspecieResponse): void {
    this.editandoEspecieId = especie.id;
    this.formEspecie = { nombre: especie.nombre };
    this.abrirModal(this.modalEspecie);
  }

  guardarEspecie(): void {
    const nombre = this.formEspecie.nombre.trim();
    if (!nombre) {
      this.errorModal.set('El nombre de la especie es obligatorio.');
      return;
    }
    if (nombre.length > 100) {
      this.errorModal.set('El nombre no puede superar 100 caracteres.');
      return;
    }

    this.guardando.set(true);
    const peticion: Observable<unknown> = this.editandoEspecieId
      ? this.adminService.actualizarEspecie(this.editandoEspecieId, { nombre })
      : this.adminService.crearEspecie({ nombre });

    peticion.subscribe({
      next: () => this.alGuardar(this.modalEspecie, this.editandoEspecieId ? 'Especie actualizada.' : 'Especie agregada.'),
      error: (err) => this.alFallarGuardado(err, 'No se pudo guardar la especie.')
    });
  }

  // ---------- Razas ----------

  abrirNuevaRaza(): void {
    this.editandoRazaId = null;
    this.formRaza = { nombre: '', especieId: this.especieSeleccionadaId() ?? 0 };
    this.abrirModal(this.modalRaza);
  }

  abrirEditarRaza(raza: RazaResponse): void {
    this.editandoRazaId = raza.id;
    this.formRaza = { nombre: raza.nombre, especieId: raza.especieId };
    this.abrirModal(this.modalRaza);
  }

  guardarRaza(): void {
    const nombre = this.formRaza.nombre.trim();
    if (!nombre || !this.formRaza.especieId) {
      this.errorModal.set('Completá el nombre y la especie de la raza.');
      return;
    }
    if (nombre.length > 100) {
      this.errorModal.set('El nombre no puede superar 100 caracteres.');
      return;
    }

    this.guardando.set(true);
    const payload = { nombre, especieId: this.formRaza.especieId };
    const peticion: Observable<unknown> = this.editandoRazaId
      ? this.adminService.actualizarRaza(this.editandoRazaId, payload)
      : this.adminService.crearRaza(payload);

    peticion.subscribe({
      next: () => this.alGuardar(this.modalRaza, this.editandoRazaId ? 'Raza actualizada.' : 'Raza agregada.'),
      error: (err) => this.alFallarGuardado(err, 'No se pudo guardar la raza.')
    });
  }

  // ---------- Eliminar (especie o raza) ----------

  pedirEliminar(tipo: 'especie' | 'raza', id: number, nombre: string): void {
    this.limpiarMensajes();
    this.porEliminar.set({ tipo, id, nombre });
  }

  confirmarEliminar(): void {
    const pendiente = this.porEliminar();
    if (!pendiente) return;

    this.eliminando.set(true);
    const peticion: Observable<unknown> =
      pendiente.tipo === 'especie'
        ? this.adminService.eliminarEspecie(pendiente.id)
        : this.adminService.eliminarRaza(pendiente.id);

    peticion.subscribe({
      next: () => {
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeExito.set(`${pendiente.tipo === 'especie' ? 'Especie' : 'Raza'} “${pendiente.nombre}” eliminada.`);
        this.cargarDatos();
      },
      error: (err) => {
        // La API responde 409 si el registro está en uso (regla general de los catálogos)
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeError.set(mensajeDeError(err, 'No se pudo eliminar el registro.'));
      }
    });
  }

  // ---------- Utilidades ----------

  cerrarModales(): void {
    this.modalEspecie.set(false);
    this.modalRaza.set(false);
  }

  private abrirModal(modal: WritableSignal<boolean>): void {
    this.limpiarMensajes();
    this.errorModal.set('');
    modal.set(true);
  }

  private alGuardar(modal: WritableSignal<boolean>, mensaje: string): void {
    this.guardando.set(false);
    modal.set(false);
    this.mensajeExito.set(mensaje);
    this.cargarDatos();
  }

  private alFallarGuardado(err: unknown, porDefecto: string): void {
    this.guardando.set(false);
    this.errorModal.set(mensajeDeError(err, porDefecto));
  }

  private limpiarMensajes(): void {
    this.mensajeExito.set('');
    this.mensajeError.set('');
  }
}
