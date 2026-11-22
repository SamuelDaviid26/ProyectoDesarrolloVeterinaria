import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { AdminService } from '../../../core/services/admin.service';
import { RolUsuario, UsuarioResponse, VeterinarioResponse } from '../../../core/models/api.models';
import { mensajeDeError } from '../../../core/utils/http-error';
import { ConfirmarDialogoComponent } from '../../../shared/confirmar-dialogo/confirmar-dialogo.component';

type Accion = { tipo: 'eliminar' | 'bloquear' | 'desbloquear'; usuario: UsuarioResponse };

/**
 * Secciones 3.D y 3.E del enunciado: administrar usuarios y bloquearlos/desbloquearlos.
 * Un usuario con perfil Veterinario debe quedar vinculado a un veterinario registrado.
 */
@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [FormsModule, ConfirmarDialogoComponent],
  templateUrl: './usuarios.component.html'
})
export class UsuariosComponent implements OnInit {
  readonly roles: RolUsuario[] = ['Administrador', 'Veterinario', 'Dueño'];

  readonly usuarios = signal<UsuarioResponse[]>([]);
  readonly veterinarios = signal<VeterinarioResponse[]>([]);
  readonly cargando = signal(false);
  readonly mensajeExito = signal('');
  readonly mensajeError = signal('');

  filtros = { nombre: '', cedula: '', rol: '', estado: '' as '' | 'activos' | 'bloqueados' };

  readonly modalAbierto = signal(false);
  readonly errorModal = signal('');
  readonly guardando = signal(false);
  editandoId: string | null = null;
  form = this.formVacio();

  readonly accionPendiente = signal<Accion | null>(null);
  readonly procesando = signal(false);

  /** Mapa id → nombre para mostrar a qué veterinario está vinculada cada cuenta. */
  readonly nombreVeterinario = computed(() => {
    const mapa: Record<number, string> = {};
    for (const vet of this.veterinarios()) mapa[vet.id] = vet.nombreCompleto;
    return mapa;
  });

  constructor(private readonly adminService: AdminService) {}

  ngOnInit(): void {
    this.cargarVeterinarios();
    this.buscar();
  }

  buscar(): void {
    this.cargando.set(true);
    this.mensajeError.set('');
    this.adminService
      .listarUsuarios({
        nombre: this.filtros.nombre,
        cedula: this.filtros.cedula,
        rol: this.filtros.rol,
        bloqueado: this.filtros.estado === '' ? null : this.filtros.estado === 'bloqueados'
      })
      .subscribe({
        next: (usuarios) => {
          this.usuarios.set(usuarios);
          this.cargando.set(false);
        },
        error: (err) => {
          this.mensajeError.set(mensajeDeError(err, 'No se pudieron cargar los usuarios.'));
          this.cargando.set(false);
        }
      });
  }

  limpiarFiltros(): void {
    this.filtros = { nombre: '', cedula: '', rol: '', estado: '' };
    this.buscar();
  }

  /**
   * Veterinarios que se pueden vincular: los que no tienen cuenta,
   * más el que ya tiene el usuario que se está editando.
   */
  veterinariosDisponibles(): VeterinarioResponse[] {
    const ocupados = new Set(
      this.usuarios()
        .filter((u) => u.veterinarioId !== null && u.id !== this.editandoId)
        .map((u) => u.veterinarioId)
    );
    return this.veterinarios().filter((v) => !ocupados.has(v.id));
  }

  // ---------- Crear / editar ----------

  abrirNuevo(): void {
    this.editandoId = null;
    this.form = this.formVacio();
    this.abrirModal();
  }

  abrirEditar(usuario: UsuarioResponse): void {
    this.editandoId = usuario.id;
    this.form = {
      cedula: usuario.cedula,
      nombreCompleto: usuario.nombreCompleto,
      correo: usuario.correo,
      rol: usuario.rol as RolUsuario,
      veterinarioId: usuario.veterinarioId,
      contrasena: ''
    };
    this.abrirModal();
  }

  alCambiarRol(): void {
    // Solo el perfil Veterinario lleva vínculo con una ficha de veterinario
    if (this.form.rol !== 'Veterinario') {
      this.form.veterinarioId = null;
    }
  }

  guardar(): void {
    const error = this.validarFormulario();
    if (error) {
      this.errorModal.set(error);
      return;
    }

    const datos = {
      cedula: this.form.cedula.trim(),
      nombreCompleto: this.form.nombreCompleto.trim(),
      correo: this.form.correo.trim(),
      rol: this.form.rol,
      veterinarioId: this.form.rol === 'Veterinario' ? this.form.veterinarioId : null
    };

    this.guardando.set(true);
    const peticion: Observable<unknown> = this.editandoId
      ? this.adminService.actualizarUsuario(this.editandoId, {
          ...datos,
          // Si se deja vacía, la API conserva la contraseña actual
          contrasena: this.form.contrasena ? this.form.contrasena : null
        })
      : this.adminService.crearUsuario({ ...datos, contrasena: this.form.contrasena });

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.modalAbierto.set(false);
        this.mensajeExito.set(this.editandoId ? 'Usuario actualizado.' : 'Usuario creado.');
        this.buscar();
      },
      error: (err) => {
        this.guardando.set(false);
        this.errorModal.set(
          mensajeDeError(
            err,
            'No se pudo guardar el usuario. Revisá que la cédula, el correo y el veterinario no estén en uso.'
          )
        );
      }
    });
  }

  // ---------- Bloquear / desbloquear / eliminar ----------

  pedirAccion(tipo: Accion['tipo'], usuario: UsuarioResponse): void {
    this.limpiarMensajes();
    this.accionPendiente.set({ tipo, usuario });
  }

  textoConfirmacion(accion: Accion): string {
    const nombre = accion.usuario.nombreCompleto;
    switch (accion.tipo) {
      case 'bloquear':
        return `${nombre} no podrá iniciar sesión hasta que un administrador lo desbloquee.`;
      case 'desbloquear':
        return `${nombre} podrá volver a iniciar sesión.`;
      default:
        return `¿Seguro que querés eliminar a ${nombre}? Solo se pueden eliminar cuentas sin registros asociados.`;
    }
  }

  confirmarAccion(): void {
    const accion = this.accionPendiente();
    if (!accion) return;

    const { tipo, usuario } = accion;
    const peticion: Observable<unknown> =
      tipo === 'eliminar'
        ? this.adminService.eliminarUsuario(usuario.id)
        : this.adminService.cambiarBloqueo(usuario.id, tipo === 'bloquear');

    this.procesando.set(true);
    peticion.subscribe({
      next: () => {
        this.procesando.set(false);
        this.accionPendiente.set(null);
        const textos = { eliminar: 'eliminado', bloquear: 'bloqueado', desbloquear: 'desbloqueado' };
        this.mensajeExito.set(`${usuario.nombreCompleto} fue ${textos[tipo]}.`);
        this.buscar();
      },
      error: (err) => {
        this.procesando.set(false);
        this.accionPendiente.set(null);
        this.mensajeError.set(this.mensajeConflicto(err, tipo));
      }
    });
  }

  // ---------- Utilidades ----------

  private mensajeConflicto(err: unknown, tipo: Accion['tipo']): string {
    const status = (err as { status?: number })?.status;
    if (status === 409 && tipo === 'eliminar') {
      return 'No se puede eliminar: el usuario tiene mascotas o registros clínicos asociados, o es el último administrador activo. Podés bloquearlo en su lugar.';
    }
    if (status === 409 && tipo === 'bloquear') {
      return 'No se puede bloquear al último administrador activo del sistema.';
    }
    return mensajeDeError(err, 'No se pudo completar la operación.');
  }

  private validarFormulario(): string | null {
    const f = this.form;
    if (!f.cedula.trim() || !f.nombreCompleto.trim() || !f.correo.trim()) {
      return 'La cédula, el nombre y el correo son obligatorios.';
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.correo.trim())) {
      return 'El correo electrónico no es válido.';
    }
    if (!this.editandoId && !f.contrasena) {
      return 'La contraseña es obligatoria al crear un usuario.';
    }
    if (f.rol === 'Veterinario' && !f.veterinarioId) {
      return 'Un usuario con perfil Veterinario debe quedar vinculado a un veterinario registrado.';
    }
    return null;
  }

  private cargarVeterinarios(): void {
    this.adminService.listarVeterinarios().subscribe({
      next: (vets) => this.veterinarios.set(vets),
      error: () => this.veterinarios.set([])
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

  private formVacio() {
    return {
      cedula: '',
      nombreCompleto: '',
      correo: '',
      rol: 'Dueño' as RolUsuario,
      veterinarioId: null as number | null,
      contrasena: ''
    };
  }
}
