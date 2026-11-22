import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import {
  MascotaFormPayload,
  VeterinariaService
} from '../../../core/services/veterinaria.service';
import {
  DuenoResponse,
  EspecieResponse,
  MascotaListadoResponse,
  RazaResponse
} from '../../../core/models/api.models';

@Component({
  selector: 'app-mascotas-listado',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './mascotas-listado.component.html',
  styleUrl: './mascotas-listado.component.css'
})
export class MascotasListadoComponent implements OnInit, OnDestroy {
  rolActual = '';
  filtroNombre = '';
  filtroCedula = '';

  atencionesRecientes: MascotaListadoResponse[] = [];
  sinAtenciones: MascotaListadoResponse[] = [];
  fotosUrl: Record<number, string> = {};

  cargando = false;
  mensajeError = '';

  modalAbierto = false;
  editandoMascotaId: number | null = null;
  guardandoMascota = false;
  errorModal = '';

  especies: EspecieResponse[] = [];
  todasLasRazas: RazaResponse[] = [];
  razasFiltradas: RazaResponse[] = [];
  especieSeleccionadaId: number | null = null;

  cedulaBusquedaDueno = '';
  duenoEncontrado: DuenoResponse | null = null;
  nuevoDueno = {
    cedula: '',
    nombreCompleto: '',
    correo: '',
    contrasena: ''
  };

  formMascota: MascotaFormPayload = {
    nombre: '',
    razaId: 0,
    fechaNacimiento: '',
    sexo: '',
    duenoId: '',
    foto: null
  };

  constructor(
    private readonly vetService: VeterinariaService,
    private readonly authService: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  async ngOnInit(): Promise<void> {
    this.rolActual = this.authService.obtenerRol() ?? 'Veterinario';
    await Promise.all([this.cargarMascotas(), this.cargarCatalogosBase()]);
    this.cdr.detectChanges();
  }

  ngOnDestroy(): void {
    this.limpiarObjectUrls();
  }

  async buscarMascotas(): Promise<void> {
    await this.cargarMascotas();
  }

  async limpiarFiltros(): Promise<void> {
    this.filtroNombre = '';
    this.filtroCedula = '';
    await this.cargarMascotas();
  }

  cerrarSesion(): void {
    this.authService.logout(true);
  }

  async cargarMascotas(): Promise<void> {
    this.cargando = true;
    this.mensajeError = '';
    this.cdr.detectChanges();

    try {
      const res = await firstValueFrom(
        this.vetService.listarMascotas({
          nombre: this.filtroNombre,
          cedulaDueno: this.filtroCedula
        })
      );
      this.limpiarObjectUrls();
      this.atencionesRecientes = res.atencionesRecientes ?? [];
      this.sinAtenciones = res.sinAtenciones ?? [];

      for (const m of [...this.atencionesRecientes, ...this.sinAtenciones]) {
        if (m.fotoUrl) {
          void this.cargarFotoSegura(m.id, m.fotoUrl);
        }
      }
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo cargar el listado de mascotas.';
    } finally {
      this.cargando = false;
      this.cdr.detectChanges();
    }
  }

  private async cargarFotoSegura(mascotaId: number, fotoUrl: string): Promise<void> {
    try {
      const blob = await firstValueFrom(this.vetService.obtenerArchivoBlob(fotoUrl));
      this.fotosUrl[mascotaId] = URL.createObjectURL(blob);
      this.cdr.detectChanges();
    } catch {
    }
  }

  private limpiarObjectUrls(): void {
    for (const url of Object.values(this.fotosUrl)) {
      URL.revokeObjectURL(url);
    }
    this.fotosUrl = {};
  }

  private async cargarCatalogosBase(): Promise<void> {
    try {
      this.especies = await firstValueFrom(this.vetService.listarEspecies());
    } catch {
    }

    try {
      const razas = await firstValueFrom(this.vetService.listarRazas());
      this.todasLasRazas = razas;
      this.razasFiltradas = razas;
    } catch {
    }

    this.cdr.detectChanges();
  }

  abrirModalCrear(): void {
    this.editandoMascotaId = null;
    this.errorModal = '';
    this.duenoEncontrado = null;
    this.cedulaBusquedaDueno = '';
    this.especieSeleccionadaId = null;
    this.razasFiltradas = this.todasLasRazas;
    this.formMascota = {
      nombre: '',
      razaId: 0,
      fechaNacimiento: '',
      sexo: '',
      duenoId: '',
      foto: null
    };
    this.modalAbierto = true;
  }

  async abrirModalEditar(mascotaId: number): Promise<void> {
    this.editandoMascotaId = mascotaId;
    this.errorModal = '';
    try {
      const m = await firstValueFrom(this.vetService.obtenerMascota(mascotaId));
      this.especieSeleccionadaId = m.especieId;
      this.razasFiltradas = this.todasLasRazas.filter((r) => r.especieId === m.especieId);
      this.duenoEncontrado = {
        id: m.duenoId,
        cedula: m.duenoCedula,
        nombreCompleto: m.duenoNombreCompleto
      };
      this.cedulaBusquedaDueno = m.duenoCedula;
      this.formMascota = {
        nombre: m.nombre,
        razaId: m.razaId,
        fechaNacimiento: m.fechaNacimiento,
        sexo: m.sexo,
        duenoId: m.duenoId,
        foto: null
      };
      this.modalAbierto = true;
      this.cdr.detectChanges();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo cargar la mascota.';
      this.cdr.detectChanges();
    }
  }

  cerrarModal(): void {
    this.modalAbierto = false;
    this.errorModal = '';
  }

  alCambiarEspecie(especieId: number | null): void {
    this.formMascota.razaId = 0;
    this.razasFiltradas = especieId
      ? this.todasLasRazas.filter((r) => r.especieId === especieId)
      : this.todasLasRazas;
  }

  async buscarDueno(): Promise<void> {
    this.errorModal = '';
    if (!this.cedulaBusquedaDueno.trim()) {
      this.errorModal = 'Ingresá una cédula para buscar al dueño.';
      return;
    }

    try {
      const dueno = await firstValueFrom(
        this.vetService.buscarDuenoPorCedula(this.cedulaBusquedaDueno)
      );
      this.duenoEncontrado = dueno;
      this.formMascota.duenoId = dueno.id;
    } catch (err: unknown) {
      this.duenoEncontrado = null;
      this.formMascota.duenoId = '';
      const httpErr = err as { status?: number; error?: { mensaje?: string } };
      this.errorModal =
        httpErr?.status === 404
          ? 'No se encontró un dueño con esa cédula. Podés crearlo abajo.'
          : httpErr?.error?.mensaje || 'Error al buscar el dueño.';
    } finally {
      this.cdr.detectChanges();
    }
  }

  async crearDuenoRapido(): Promise<void> {
    this.errorModal = '';
    if (
      !this.nuevoDueno.cedula.trim() ||
      !this.nuevoDueno.nombreCompleto.trim() ||
      !this.nuevoDueno.correo.trim() ||
      !this.nuevoDueno.contrasena
    ) {
      this.errorModal = 'Completá cédula, nombre, correo y contraseña.';
      return;
    }

    try {
      const dueno = await firstValueFrom(this.vetService.crearDueno(this.nuevoDueno));
      this.duenoEncontrado = dueno;
      this.formMascota.duenoId = dueno.id;
      this.cedulaBusquedaDueno = dueno.cedula;
      this.nuevoDueno = { cedula: '', nombreCompleto: '', correo: '', contrasena: '' };
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string; errores?: string[] } };
      this.errorModal =
        httpErr?.error?.mensaje ||
        (Array.isArray(httpErr?.error?.errores)
          ? httpErr.error.errores.join(', ')
          : 'No se pudo crear el dueño.');
    } finally {
      this.cdr.detectChanges();
    }
  }

  onArchivoSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.formMascota.foto = input.files && input.files.length > 0 ? input.files[0] : null;
  }

  async guardarMascota(): Promise<void> {
    this.errorModal = '';

    if (
      !this.formMascota.nombre.trim() ||
      !this.formMascota.sexo ||
      !this.formMascota.fechaNacimiento
    ) {
      this.errorModal = 'Completá nombre, sexo y fecha de nacimiento.';
      return;
    }
    if (!this.formMascota.razaId || this.formMascota.razaId <= 0) {
      this.errorModal = 'Seleccioná una raza válida.';
      return;
    }
    if (!this.formMascota.duenoId) {
      this.errorModal = 'Buscá o creá un dueño antes de guardar.';
      return;
    }
    if (!this.editandoMascotaId && !this.formMascota.foto) {
      this.errorModal = 'La fotografía es obligatoria al registrar una nueva mascota.';
      return;
    }

    this.guardandoMascota = true;
    this.cdr.detectChanges();
    try {
      if (this.editandoMascotaId) {
        await firstValueFrom(
          this.vetService.actualizarMascota(this.editandoMascotaId, this.formMascota)
        );
      } else {
        await firstValueFrom(this.vetService.crearMascota(this.formMascota));
      }

      this.modalAbierto = false;
      await this.cargarMascotas();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.errorModal = httpErr?.error?.mensaje || 'No se pudo guardar la mascota.';
    } finally {
      this.guardandoMascota = false;
      this.cdr.detectChanges();
    }
  }
}