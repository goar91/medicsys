# Odontograma Moderno 3D

## 🦷 Descripción

Componente de odontograma completamente rediseñado desde cero con un enfoque moderno, práctico y elegante. Inspirado en diseños profesionales de odontología, combina visualización anatómica y coronal de los dientes.

## ✨ Características Principales

### Diseño Visual
- **Vista Dual**: Combinación de vista anatómica (lateral) y coronal (superior) de cada diente
- **Interfaz Moderna**: Diseño limpio con efectos de hover, sombras suaves y transiciones fluidas
- **Responsive**: Se adapta perfectamente a diferentes tamaños de pantalla
- **Iconografía Clara**: Cada condición dental tiene su propio icono distintivo

### Funcionalidades

#### 1. Marcadores Dentales (9 tipos)
- 🔴 **Caries**: Indica presencia de caries dental
- 🔵 **Restauración**: Diente con empaste o restauración
- ⚪ **Ausente**: Diente faltante (nunca erupcionó)
- 🟠 **Extraído**: Diente que fue extraído
- 🟡 **Necesita Tratamiento**: Requiere atención dental
- 🟣 **Corona**: Diente con corona dental
- 🟣 **Implante**: Implante dental
- 🌸 **Endodoncia**: Tratamiento de conducto radicular

#### 2. Superficies Dentales
Cada diente permite marcar 5 superficies individuales:
- **Oclusal**: Superficie de masticación (centro)
- **Vestibular**: Superficie frontal/externa (arriba)
- **Lingual**: Superficie interna (abajo)
- **Mesial**: Superficie lateral izquierda
- **Distal**: Superficie lateral derecha

#### 3. Interactividad
- Click en superficie específica para marcar/desmarcar
- Hover para resaltar el diente
- Indicadores visuales de estado
- Numeración FDI (11-18, 21-28, 31-38, 41-48)
- Toggle de numeración on/off

### Notación FDI
Utiliza el sistema de numeración internacional FDI:
- **Cuadrante 1** (11-18): Superior derecho
- **Cuadrante 2** (21-28): Superior izquierdo
- **Cuadrante 3** (31-38): Inferior izquierdo
- **Cuadrante 4** (41-48): Inferior derecho

## 🎨 Arquitectura

### Componente Principal
```typescript
@Component({
  selector: 'app-odontogram-3d',
  standalone: true,
  imports: [NgFor, NgIf, NgClass]
})
export class Odontogram3DComponent
```

### Props de Entrada
- `state: Odontogram3DState` - Estado completo del odontograma
- `activeMarker: OdontogramMarker` - Marcador actualmente seleccionado
- `readonly: boolean` - Modo solo lectura

### Eventos de Salida
- `stateChange: EventEmitter<Odontogram3DState>` - Emite cambios en el estado

## 🔧 Uso

```html
<app-odontogram-3d
  [state]="odontogram()"
  [activeMarker]="marker()"
  [readonly]="false"
  (stateChange)="updateOdontogram($event)"
></app-odontogram-3d>
```

## 🎯 Mejoras vs Versión Anterior

### Removido
- ❌ Dependencia de Three.js (reducción significativa del bundle)
- ❌ WebGL y renderizado 3D complejo
- ❌ Controles de órbita y cámara
- ❌ Geometrías procedurales

### Agregado
- ✅ Diseño 2D moderno y limpio
- ✅ Vista dual anatómica + coronal
- ✅ 3 marcadores adicionales (corona, implante, endodoncia)
- ✅ Mejor UX con hover states y animaciones
- ✅ Mayor rendimiento (sin WebGL)
- ✅ Interfaz más intuitiva y profesional
- ✅ Mejor accesibilidad

## 💅 Estilos

El componente utiliza:
- Gradientes suaves para fondos
- Box shadows multicapa para profundidad
- Transiciones CSS cubic-bezier para animaciones fluidas
- Variables CSS para temas personalizables
- Diseño responsive con media queries

## 📱 Responsive

- **Desktop (>1200px)**: Vista completa con todos los detalles
- **Tablet (768px-1200px)**: Dientes ligeramente más pequeños
- **Mobile (<768px)**: Optimización para pantallas pequeñas, scroll horizontal si es necesario

## 🚀 Rendimiento

- Sin dependencias pesadas de 3D
- Renderizado puro HTML/CSS
- Animaciones optimizadas con GPU
- Lazy loading de imágenes/assets
- Tamaño de bundle reducido en ~500KB

## 📝 Notas de Desarrollo

- El componente es completamente standalone
- Compatible con Angular 15+
- Utiliza signals para reactividad
- Totalmente tipado con TypeScript
- Sin dependencias externas (excepto Angular)
