import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { CatalogoClinicoResponse, TipoCatalogo } from '../../../core/models/api.models';
import { mensajeDeError } from '../../../core/utils/http-error';
import { ConfirmarDialogoComponent } from '../../../shared/confirmar-dialogo/confirmar-dialogo.component';

/**
 * Sección 3.B del enunciado: medicamentos y tratamientos que se pueden asignar a una mascota.
 * La búsqueda (por nombre y tipo) la resuelve la API.
 */
@Component({
  selector: 'app-catalogo-clinico',
  standalone: true,
  imports: [FormsModule, ConfirmarDialogoComponent],
  templateUrl: './catalogo-clinico.component.html'
})
export class CatalogoClinicoComponent implements OnInit {
  readonly tipos: TipoCatalogo[] = ['Medicamento', 'Tratamiento'];

  readonly elementos = signal<CatalogoClinicoResponse[]>([]);
  readonly cargando = signal(false);
  readonly mensajeExito = signal('');
  readonly mensajeError = signal('');

  filtroNombre = '';
  filtroTipo: TipoCatalogo | '' = '';

  readonly modalAbierto = signal(false);
  readonly errorModal = signal('');
  readonly guardando = signal(false);
  editandoId: number | null = null;
  form = { nombre: '', tipo: 'Medicamento' as TipoCatalogo, descripcion: '' };

  readonly porEliminar = signal<CatalogoClinicoResponse | null>(null);
  readonly eliminando = signal(false);

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.buscar();
  }

  buscar(): void {
    this.cargando.set(true);
    this.mensajeError.set('');
    this.adminService.listarCatalogo(this.filtroNombre, this.filtroTipo).subscribe({
      next: (elementos) => {
        this.elementos.set(elementos);
        this.cargando.set(false);
      },
      error: (err) => {
        this.mensajeError.set(mensajeDeError(err, 'No se pudo cargar el catálogo clínico.'));
        this.cargando.set(false);
      }
    });
  }

  limpiarFiltros(): void {
    this.filtroNombre = '';
    this.filtroTipo = '';
    this.buscar();
  }

  abrirNuevo(): void {
    this.editandoId = null;
    this.form = { nombre: '', tipo: 'Medicamento', descripcion: '' };
    this.abrirModal();
  }

  abrirEditar(elemento: CatalogoClinicoResponse): void {
    this.editandoId = elemento.id;
    this.form = {
      nombre: elemento.nombre,
      tipo: elemento.tipo as TipoCatalogo,
      descripcion: elemento.descripcion
    };
    this.abrirModal();
  }

  guardar(): void {
    const payload = {
      nombre: this.form.nombre.trim(),
      tipo: this.form.tipo,
      descripcion: this.form.descripcion.trim()
    };
    if (!payload.nombre || !payload.descripcion) {
      this.errorModal.set('El nombre y la descripción son obligatorios.');
      return;
    }
    if (payload.nombre.length > 150 || payload.descripcion.length > 1000) {
      this.errorModal.set('El nombre admite 150 caracteres y la descripción 1000.');
      return;
    }

    this.guardando.set(true);
    const peticion: Observable<unknown> = this.editandoId
      ? this.adminService.actualizarElementoCatalogo(this.editandoId, payload)
      : this.adminService.crearElementoCatalogo(payload);

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.mensajeExito.set(this.editandoId ? 'Elemento actualizado.' : 'Elemento agregado al catálogo.');
        this.buscar();
      },
      error: (err) => {
        this.guardando.set(false);
        this.errorModal.set(mensajeDeError(err, 'No se pudo guardar el elemento.'));
      }
    });
  }

  pedirEliminar(elemento: CatalogoClinicoResponse): void {
    this.limpiarMensajes();
    this.porEliminar.set(elemento);
  }

  confirmarEliminar(): void {
    const elemento = this.porEliminar();
    if (!elemento) return;

    this.eliminando.set(true);
    this.adminService.eliminarElementoCatalogo(elemento.id).subscribe({
      next: () => {
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeExito.set(`“${elemento.nombre}” eliminado del catálogo.`);
        this.buscar();
      },
      error: (err) => {
        // 409 si ya fue asignado a alguna mascota
        this.eliminando.set(false);
        this.porEliminar.set(null);
        this.mensajeError.set(mensajeDeError(err, 'No se pudo eliminar el elemento.'));
      }
    });
  }

  private abrirModal(): void {
    this.limpiarMensajes();
    this.errorModal.set('');
    this.modalAbierto.set(true);
  }

  private limpiarMensajes(): void {
    this.mensajeExito.set('');
    this.mensajeError.set('');
  }
}
