import { ChangeDetectorRef, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subject, firstValueFrom, takeUntil } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { VeterinariaService } from '../../../core/services/veterinaria.service';
import {
  CatalogoClinicoResponse,
  ExamenResponse,
  MascotaResponse,
  NotaResponse,
  TratamientoResponse,
  VacunaResponse
} from '../../../core/models/api.models';

@Component({
  selector: 'app-expediente-mascota',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './expediente-mascota.component.html',
  styleUrl: './expediente-mascota.component.css'
})
export class ExpedienteMascotaComponent implements OnInit, OnDestroy {
  @ViewChild('editorNota') editorNota?: ElementRef<HTMLTextAreaElement>;

  private readonly destroy$ = new Subject<void>();

  mascotaId = 0;
  mascota: MascotaResponse | null = null;
  fotoMascotaUrl: string | null = null;
  cargando = false;

  pestanaActiva: 'vacunas' | 'tratamientos' | 'examenes' | 'notas' = 'vacunas';
  esAdministrador = false;

  mensajeError = '';
  mensajeExito = '';

  vacunas: VacunaResponse[] = [];
  nuevaVacuna = {
    nombreVacuna: '',
    fechaAplicacion: '',
    fechaProximaDosis: '',
    vacunaAnteriorId: null as number | null,
    veterinarioResponsableId: null as number | null
  };

  catalogoClinico: CatalogoClinicoResponse[] = [];
  tratamientosVigentes: TratamientoResponse[] = [];
  tratamientosSuspendidos: TratamientoResponse[] = [];
  nuevoTratamiento = {
    elementoCatalogoClinicoId: 0,
    veterinarioResponsableId: null as number | null
  };

  examenes: ExamenResponse[] = [];
  archivoExamenSeleccionado: File | null = null;
  nuevoExamen = {
    descripcion: '',
    veterinarioResponsableId: null as number | null
  };

  notas: NotaResponse[] = [];
  nuevaNotaHtml = '';
  veterinarioNotaId: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly vetService: VeterinariaService,
    private readonly authService: AuthService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.esAdministrador = this.authService.obtenerRol() === 'Administrador';

    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      const id = Number(params.get('id'));
      this.mascotaId = Number.isFinite(id) && id > 0 ? id : 0;

      if (this.mascotaId > 0) {
        void this.cargarTodo();
      } else {
        this.mascota = null;
        this.mensajeError = 'ID de mascota inválido.';
        this.cdr.detectChanges();
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.liberarFoto();
  }

  cerrarSesion(): void {
    this.authService.logout(true);
  }

  private liberarFoto(): void {
    if (this.fotoMascotaUrl) {
      URL.revokeObjectURL(this.fotoMascotaUrl);
      this.fotoMascotaUrl = null;
    }
  }

  private limpiarMensajes(): void {
    this.mensajeError = '';
    this.mensajeExito = '';
  }

  private refrescarVista(): void {
    this.cdr.detectChanges();
  }

  private async cargarTodo(): Promise<void> {
    this.cargando = true;
    this.limpiarMensajes();
    this.liberarFoto();
    this.refrescarVista();

    try {
      const m = await firstValueFrom(this.vetService.obtenerMascota(this.mascotaId));
      this.mascota = m;

      if (m?.fotoUrl) {
        try {
          const blob = await firstValueFrom(this.vetService.obtenerArchivoBlob(m.fotoUrl));
          this.fotoMascotaUrl = URL.createObjectURL(blob);
        } catch {
          this.fotoMascotaUrl = null;
        }
      }
    } catch (err: unknown) {
      this.mascota = null;
      const httpErr = err as { status?: number; message?: string; error?: { mensaje?: string } };
      this.mensajeError =
        httpErr?.error?.mensaje ||
        (httpErr?.status ? `Error ${httpErr.status} al cargar la mascota.` : 'No se pudo cargar la mascota.');
    }

    try {
      this.catalogoClinico = await firstValueFrom(this.vetService.listarCatalogoClinico());
    } catch {
      this.catalogoClinico = [];
    }

    await Promise.all([
      this.cargarVacunas(),
      this.cargarTratamientos(),
      this.cargarExamenes(),
      this.cargarNotas()
    ]);

    this.cargando = false;
    this.refrescarVista();
  }

  private async cargarVacunas(): Promise<void> {
    try {
      this.vacunas = await firstValueFrom(this.vetService.listarVacunas(this.mascotaId));
    } catch {
      this.vacunas = [];
    }
  }

  async registrarVacuna(): Promise<void> {
    this.validarAdmin(this.nuevaVacuna.veterinarioResponsableId);
    if (this.mensajeError) {
      this.refrescarVista();
      return;
    }
    if (!this.nuevaVacuna.nombreVacuna.trim() || !this.nuevaVacuna.fechaAplicacion) {
      this.mensajeError = 'Completá nombre de vacuna y fecha de aplicación.';
      this.refrescarVista();
      return;
    }

    try {
      await firstValueFrom(
        this.vetService.crearVacuna(this.mascotaId, {
          nombreVacuna: this.nuevaVacuna.nombreVacuna.trim(),
          fechaAplicacion: this.nuevaVacuna.fechaAplicacion,
          fechaProximaDosis: this.nuevaVacuna.fechaProximaDosis || null,
          vacunaAnteriorId: this.nuevaVacuna.vacunaAnteriorId,
          veterinarioResponsableId: this.nuevaVacuna.veterinarioResponsableId
        })
      );
      this.mensajeExito = 'Vacuna registrada correctamente.';
      this.nuevaVacuna = {
        nombreVacuna: '',
        fechaAplicacion: '',
        fechaProximaDosis: '',
        vacunaAnteriorId: null,
        veterinarioResponsableId: this.nuevaVacuna.veterinarioResponsableId
      };
      await this.cargarVacunas();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo registrar la vacuna.';
    }
    this.refrescarVista();
  }

  private async cargarTratamientos(): Promise<void> {
    try {
      const res = await firstValueFrom(this.vetService.listarTratamientos(this.mascotaId));
      this.tratamientosVigentes = res.vigentes ?? [];
      this.tratamientosSuspendidos = res.suspendidos ?? [];
    } catch {
      this.tratamientosVigentes = [];
      this.tratamientosSuspendidos = [];
    }
  }

  async asignarTratamiento(): Promise<void> {
    this.validarAdmin(this.nuevoTratamiento.veterinarioResponsableId);
    if (this.mensajeError) {
      this.refrescarVista();
      return;
    }
    if (!this.nuevoTratamiento.elementoCatalogoClinicoId) {
      this.mensajeError = 'Seleccioná un medicamento o tratamiento del catálogo.';
      this.refrescarVista();
      return;
    }

    try {
      await firstValueFrom(
        this.vetService.crearTratamiento(this.mascotaId, {
          elementoCatalogoClinicoId: this.nuevoTratamiento.elementoCatalogoClinicoId,
          veterinarioResponsableId: this.nuevoTratamiento.veterinarioResponsableId
        })
      );
      this.mensajeExito = 'Tratamiento asignado al expediente.';
      this.nuevoTratamiento.elementoCatalogoClinicoId = 0;
      await this.cargarTratamientos();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo asignar el tratamiento.';
    }
    this.refrescarVista();
  }

  async suspenderTratamiento(tratamientoId: number): Promise<void> {
    this.limpiarMensajes();
    try {
      await firstValueFrom(
        this.vetService.suspenderTratamiento(this.mascotaId, tratamientoId, {})
      );
      this.mensajeExito = 'Tratamiento suspendido.';
      await this.cargarTratamientos();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo suspender el tratamiento.';
    }
    this.refrescarVista();
  }

  private async cargarExamenes(): Promise<void> {
    try {
      this.examenes = await firstValueFrom(this.vetService.listarExamenes(this.mascotaId));
    } catch {
      this.examenes = [];
    }
  }

  onExamenSeleccionado(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.archivoExamenSeleccionado = input.files && input.files.length > 0 ? input.files[0] : null;
  }

  async subirExamen(): Promise<void> {
    this.validarAdmin(this.nuevoExamen.veterinarioResponsableId);
    if (this.mensajeError) {
      this.refrescarVista();
      return;
    }
    if (!this.nuevoExamen.descripcion.trim() || !this.archivoExamenSeleccionado) {
      this.mensajeError = 'Completá la descripción y seleccioná un archivo JPEG, PNG o PDF.';
      this.refrescarVista();
      return;
    }

    try {
      await firstValueFrom(
        this.vetService.crearExamen(this.mascotaId, {
          descripcion: this.nuevoExamen.descripcion.trim(),
          archivo: this.archivoExamenSeleccionado,
          veterinarioResponsableId: this.nuevoExamen.veterinarioResponsableId
        })
      );
      this.mensajeExito = 'Examen adjuntado correctamente.';
      this.nuevoExamen.descripcion = '';
      this.archivoExamenSeleccionado = null;
      await this.cargarExamenes();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo subir el examen.';
    }
    this.refrescarVista();
  }

  async descargarExamen(examen: ExamenResponse): Promise<void> {
    try {
      const blob = await firstValueFrom(this.vetService.obtenerArchivoBlob(examen.archivoUrl));
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = examen.archivoNombre || `examen-${examen.id}`;
      link.click();
      URL.revokeObjectURL(url);
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo descargar el archivo.';
      this.refrescarVista();
    }
  }

  private async cargarNotas(): Promise<void> {
    try {
      this.notas = await firstValueFrom(this.vetService.listarNotas(this.mascotaId));
    } catch {
      this.notas = [];
    }
  }

  envolverSeleccion(etiqueta: 'strong' | 'em'): void {
    const el = this.editorNota?.nativeElement;
    if (!el) return;
    const inicio = el.selectionStart ?? 0;
    const fin = el.selectionEnd ?? 0;
    const seleccionado = this.nuevaNotaHtml.slice(inicio, fin) || 'texto';
    this.nuevaNotaHtml =
      this.nuevaNotaHtml.slice(0, inicio) +
      `<${etiqueta}>${seleccionado}</${etiqueta}>` +
      this.nuevaNotaHtml.slice(fin);
  }

  insertarPlantillaLista(): void {
    this.nuevaNotaHtml = `${this.nuevaNotaHtml}\n<ul><li>Punto clínico 1</li><li>Punto clínico 2</li></ul>`.trim();
  }

  async guardarNota(): Promise<void> {
    this.validarAdmin(this.veterinarioNotaId);
    if (this.mensajeError) {
      this.refrescarVista();
      return;
    }
    if (!this.nuevaNotaHtml.trim()) {
      this.mensajeError = 'Escribí el contenido de la nota clínica.';
      this.refrescarVista();
      return;
    }

    try {
      await firstValueFrom(
        this.vetService.crearNota(this.mascotaId, {
          contenidoHtml: this.nuevaNotaHtml.trim(),
          veterinarioResponsableId: this.veterinarioNotaId
        })
      );
      this.mensajeExito = 'Nota clínica registrada.';
      this.nuevaNotaHtml = '';
      await this.cargarNotas();
    } catch (err: unknown) {
      const httpErr = err as { error?: { mensaje?: string } };
      this.mensajeError = httpErr?.error?.mensaje || 'No se pudo guardar la nota clínica.';
    }
    this.refrescarVista();
  }

  private validarAdmin(vetId: number | null): void {
    this.limpiarMensajes();
    if (this.esAdministrador && (!vetId || vetId <= 0)) {
      this.mensajeError = 'Como Administrador, indicá un ID de veterinario responsable válido.';
    }
  }
}