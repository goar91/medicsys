import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, firstValueFrom } from 'rxjs';

export interface SRIConfig {
  ambiente: 'pruebas' | 'produccion';
  tipoEmision: 'normal' | 'contingencia';
  razonSocial: string;
  nombreComercial: string;
  ruc: string;
  dirMatriz: string;
  contribuyenteEspecial?: string;
  obligadoContabilidad: 'SI' | 'NO';
}

export interface ComprobanteElectronico {
  infoTributaria: {
    ambiente: '1' | '2'; // 1=Pruebas, 2=Producción
    tipoEmision: '1'; // 1=Normal
    razonSocial: string;
    nombreComercial: string;
    ruc: string;
    claveAcceso: string;
    codDoc: '01'; // 01=Factura
    estab: string; // Establecimiento (3 dígitos)
    ptoEmi: string; // Punto emisión (3 dígitos)
    secuencial: string; // Secuencial (9 dígitos)
    dirMatriz: string;
  };
  infoFactura: {
    fechaEmision: string; // dd/mm/yyyy
    dirEstablecimiento: string;
    contribuyenteEspecial?: string;
    obligadoContabilidad: 'SI' | 'NO';
    tipoIdentificacionComprador: string;
    razonSocialComprador: string;
    identificacionComprador: string;
    direccionComprador?: string;
    totalSinImpuestos: number;
    totalDescuento: number;
    totalConImpuestos: Array<{
      codigo: '2'; // 2=IVA
      codigoPorcentaje: '2' | '0'; // 2=15%, 0=0%
      baseImponible: number;
      valor: number;
    }>;
    propina: number;
    importeTotal: number;
    moneda: 'DOLAR';
    pagos: Array<{
      formaPago: '01' | '16' | '17' | '19' | '20'; // 01=Efectivo, 16=Tarjeta débito, 19=Tarjeta crédito, 20=Otros
      total: number;
      plazo?: string;
      unidadTiempo?: string;
    }>;
  };
  detalles: {
    detalle: Array<{
      codigoPrincipal: string;
      descripcion: string;
      cantidad: number;
      precioUnitario: number;
      descuento: number;
      precioTotalSinImpuesto: number;
      impuestos: {
        impuesto: Array<{
          codigo: '2'; // 2=IVA
          codigoPorcentaje: '2' | '0';
          tarifa: number;
          baseImponible: number;
          valor: number;
        }>;
      };
    }>;
  };
  infoAdicional?: {
    campoAdicional: Array<{
      '@nombre': string;
      '#text': string;
    }>;
  };
}

export interface RespuestaSRI {
  estado: 'RECIBIDA' | 'DEVUELTA' | 'AUTORIZADO' | 'NO AUTORIZADO';
  numeroComprobantes?: string;
  claveAccesoConsultada?: string;
  autorizaciones?: {
    autorizacion: {
      estado: string;
      numeroAutorizacion: string;
      fechaAutorizacion: string;
      ambiente: string;
      comprobante: string;
      mensajes?: any;
    };
  };
  mensajes?: Array<{
    identificador: string;
    mensaje: string;
    informacionAdicional: string;
    tipo: 'ERROR' | 'ADVERTENCIA';
  }>;
}

/**
 * Servicio de integración con el SRI (Servicio de Rentas Internas) de Ecuador
 * para el envío y autorización de comprobantes electrónicos
 */
@Injectable({
  providedIn: 'root'
})
export class SriService {
  private readonly http = inject(HttpClient);
  
  // URLs del SRI (Web Services)
  private readonly SRI_URLS = {
    pruebas: {
      recepcion: 'https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline',
      autorizacion: 'https://celcer.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline'
    },
    produccion: {
      recepcion: 'https://cel.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline',
      autorizacion: 'https://cel.sri.gob.ec/comprobantes-electronicos-ws/AutorizacionComprobantesOffline'
    }
  };

  // Configuración del contribuyente (debe venir desde el backend)
  private config: SRIConfig = {
    ambiente: 'pruebas',
    tipoEmision: 'normal',
    razonSocial: 'CONSULTORIO DENTAL DR. CARLOS MENDOZA',
    nombreComercial: 'MEDICSYS Dental',
    ruc: '0999999999001',
    dirMatriz: 'Av. Principal 123 y Secundaria, Cuenca - Ecuador',
    obligadoContabilidad: 'SI'
  };

  /**
   * Genera la clave de acceso de 49 dígitos según normativa SRI
   */
  generarClaveAcceso(
    fecha: Date,
    tipoComprobante: '01' | '04' | '05' | '06' | '07', // 01=Factura
    ruc: string,
    ambiente: '1' | '2',
    establecimiento: string,
    puntoEmision: string,
    secuencial: string,
    codigoNumerico: string = '12345678',
    tipoEmision: '1' = '1'
  ): string {
    const dia = fecha.getDate().toString().padStart(2, '0');
    const mes = (fecha.getMonth() + 1).toString().padStart(2, '0');
    const anio = fecha.getFullYear().toString();
    
    const claveBase = 
      dia + mes + anio + 
      tipoComprobante +
      ruc +
      ambiente +
      establecimiento +
      puntoEmision +
      secuencial +
      codigoNumerico +
      tipoEmision;
    
    const digitoVerificador = this.calcularDigitoVerificadorModulo11(claveBase);
    
    return claveBase + digitoVerificador;
  }

  /**
   * Calcula el dígito verificador usando módulo 11
   */
  private calcularDigitoVerificadorModulo11(cadena: string): string {
    let factor = 2;
    let suma = 0;
    
    for (let i = cadena.length - 1; i >= 0; i--) {
      suma += parseInt(cadena[i]) * factor;
      factor = factor === 7 ? 2 : factor + 1;
    }
    
    const residuo = suma % 11;
    const resultado = residuo === 0 ? 0 : 11 - residuo;
    
    return resultado.toString();
  }

  /**
   * Convierte el objeto JavaScript a XML según el esquema del SRI
   */
  private convertirAXML(comprobante: ComprobanteElectronico): string {
    // Esta es una implementación simplificada
    // En producción se debe usar una librería XML o generar el XML completo según el XSD del SRI
    
    return `<?xml version="1.0" encoding="UTF-8"?>
<factura id="comprobante" version="1.0.0">
  <infoTributaria>
    <ambiente>${comprobante.infoTributaria.ambiente}</ambiente>
    <tipoEmision>${comprobante.infoTributaria.tipoEmision}</tipoEmision>
    <razonSocial>${this.escapeXML(comprobante.infoTributaria.razonSocial)}</razonSocial>
    <nombreComercial>${this.escapeXML(comprobante.infoTributaria.nombreComercial)}</nombreComercial>
    <ruc>${comprobante.infoTributaria.ruc}</ruc>
    <claveAcceso>${comprobante.infoTributaria.claveAcceso}</claveAcceso>
    <codDoc>${comprobante.infoTributaria.codDoc}</codDoc>
    <estab>${comprobante.infoTributaria.estab}</estab>
    <ptoEmi>${comprobante.infoTributaria.ptoEmi}</ptoEmi>
    <secuencial>${comprobante.infoTributaria.secuencial}</secuencial>
    <dirMatriz>${this.escapeXML(comprobante.infoTributaria.dirMatriz)}</dirMatriz>
  </infoTributaria>
  <infoFactura>
    <fechaEmision>${comprobante.infoFactura.fechaEmision}</fechaEmision>
    <dirEstablecimiento>${this.escapeXML(comprobante.infoFactura.dirEstablecimiento)}</dirEstablecimiento>
    <obligadoContabilidad>${comprobante.infoFactura.obligadoContabilidad}</obligadoContabilidad>
    <tipoIdentificacionComprador>${comprobante.infoFactura.tipoIdentificacionComprador}</tipoIdentificacionComprador>
    <razonSocialComprador>${this.escapeXML(comprobante.infoFactura.razonSocialComprador)}</razonSocialComprador>
    <identificacionComprador>${comprobante.infoFactura.identificacionComprador}</identificacionComprador>
    <totalSinImpuestos>${comprobante.infoFactura.totalSinImpuestos.toFixed(2)}</totalSinImpuestos>
    <totalDescuento>${comprobante.infoFactura.totalDescuento.toFixed(2)}</totalDescuento>
    <totalConImpuestos>
      ${comprobante.infoFactura.totalConImpuestos.map(imp => `
      <totalImpuesto>
        <codigo>${imp.codigo}</codigo>
        <codigoPorcentaje>${imp.codigoPorcentaje}</codigoPorcentaje>
        <baseImponible>${imp.baseImponible.toFixed(2)}</baseImponible>
        <valor>${imp.valor.toFixed(2)}</valor>
      </totalImpuesto>`).join('')}
    </totalConImpuestos>
    <propina>${comprobante.infoFactura.propina.toFixed(2)}</propina>
    <importeTotal>${comprobante.infoFactura.importeTotal.toFixed(2)}</importeTotal>
    <moneda>${comprobante.infoFactura.moneda}</moneda>
    <pagos>
      ${comprobante.infoFactura.pagos.map(pago => `
      <pago>
        <formaPago>${pago.formaPago}</formaPago>
        <total>${pago.total.toFixed(2)}</total>
      </pago>`).join('')}
    </pagos>
  </infoFactura>
  <detalles>
    ${comprobante.detalles.detalle.map(det => `
    <detalle>
      <codigoPrincipal>${this.escapeXML(det.codigoPrincipal)}</codigoPrincipal>
      <descripcion>${this.escapeXML(det.descripcion)}</descripcion>
      <cantidad>${det.cantidad}</cantidad>
      <precioUnitario>${det.precioUnitario.toFixed(2)}</precioUnitario>
      <descuento>${det.descuento.toFixed(2)}</descuento>
      <precioTotalSinImpuesto>${det.precioTotalSinImpuesto.toFixed(2)}</precioTotalSinImpuesto>
      <impuestos>
        ${det.impuestos.impuesto.map(imp => `
        <impuesto>
          <codigo>${imp.codigo}</codigo>
          <codigoPorcentaje>${imp.codigoPorcentaje}</codigoPorcentaje>
          <tarifa>${imp.tarifa}</tarifa>
          <baseImponible>${imp.baseImponible.toFixed(2)}</baseImponible>
          <valor>${imp.valor.toFixed(2)}</valor>
        </impuesto>`).join('')}
      </impuestos>
    </detalle>`).join('')}
  </detalles>
</factura>`;
  }

  private escapeXML(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  }

  /**
   * Envía el comprobante al SRI para su recepción
   */
  async enviarComprobante(comprobante: ComprobanteElectronico): Promise<RespuestaSRI> {
    const ambiente = this.config.ambiente;
    const url = this.SRI_URLS[ambiente].recepcion;
    
    const xml = this.convertirAXML(comprobante);
    
    // En producción, esto debe firmarse digitalmente con el certificado .p12
    const xmlFirmado = xml; // Aquí iría la firma digital
    
    try {
      const respuesta = await firstValueFrom(
        this.http.post<RespuestaSRI>(url, xmlFirmado, {
          headers: new HttpHeaders({
            'Content-Type': 'text/xml',
            'SOAPAction': ''
          })
        })
      );
      
      return respuesta;
    } catch (error) {
      console.error('Error al enviar comprobante al SRI:', error);
      throw error;
    }
  }

  /**
   * Consulta el estado de autorización de un comprobante
   */
  async consultarAutorizacion(claveAcceso: string): Promise<RespuestaSRI> {
    const ambiente = this.config.ambiente;
    const url = this.SRI_URLS[ambiente].autorizacion;
    
    try {
      const respuesta = await firstValueFrom(
        this.http.get<RespuestaSRI>(`${url}?claveAcceso=${claveAcceso}`)
      );
      
      return respuesta;
    } catch (error) {
      console.error('Error al consultar autorización SRI:', error);
      throw error;
    }
  }

  /**
   * Proceso completo: genera, envía y espera autorización
   */
  async procesarFactura(datosFactura: any): Promise<{
    claveAcceso: string;
    numeroAutorizacion?: string;
    estado: string;
    mensajes?: any[];
  }> {
    // 1. Generar clave de acceso
    const fecha = new Date();
    const claveAcceso = this.generarClaveAcceso(
      fecha,
      '01', // Factura
      this.config.ruc,
      this.config.ambiente === 'pruebas' ? '1' : '2',
      '001', // Establecimiento
      '001', // Punto emisión
      datosFactura.secuencial.toString().padStart(9, '0')
    );

    // 2. Construir comprobante electrónico
    const comprobante: ComprobanteElectronico = {
      infoTributaria: {
        ambiente: this.config.ambiente === 'pruebas' ? '1' : '2',
        tipoEmision: '1',
        razonSocial: this.config.razonSocial,
        nombreComercial: this.config.nombreComercial,
        ruc: this.config.ruc,
        claveAcceso: claveAcceso,
        codDoc: '01',
        estab: '001',
        ptoEmi: '001',
        secuencial: datosFactura.secuencial.toString().padStart(9, '0'),
        dirMatriz: this.config.dirMatriz
      },
      infoFactura: {
        fechaEmision: `${fecha.getDate().toString().padStart(2, '0')}/${(fecha.getMonth() + 1).toString().padStart(2, '0')}/${fecha.getFullYear()}`,
        dirEstablecimiento: this.config.dirMatriz,
        obligadoContabilidad: this.config.obligadoContabilidad,
        tipoIdentificacionComprador: datosFactura.cliente.tipoIdentificacion,
        razonSocialComprador: datosFactura.cliente.nombre,
        identificacionComprador: datosFactura.cliente.identificacion,
        totalSinImpuestos: datosFactura.subtotal,
        totalDescuento: 0,
        totalConImpuestos: [{
          codigo: '2',
          codigoPorcentaje: '2',
          baseImponible: datosFactura.subtotal,
          valor: datosFactura.iva
        }],
        propina: 0,
        importeTotal: datosFactura.total,
        moneda: 'DOLAR',
        pagos: [{
          formaPago: this.mapearFormaPago(datosFactura.formaPago),
          total: datosFactura.total
        }]
      },
      detalles: {
        detalle: datosFactura.items.map((item: any, index: number) => ({
          codigoPrincipal: `SERV-${(index + 1).toString().padStart(4, '0')}`,
          descripcion: item.descripcion,
          cantidad: item.cantidad,
          precioUnitario: item.precioUnitario,
          descuento: (item.cantidad * item.precioUnitario * item.descuento) / 100,
          precioTotalSinImpuesto: item.subtotal,
          impuestos: {
            impuesto: [{
              codigo: '2',
              codigoPorcentaje: '2',
              tarifa: 15,
              baseImponible: item.subtotal,
              valor: item.subtotal * 0.15
            }]
          }
        }))
      }
    };

    // 3. Enviar al SRI
    const respuestaRecepcion = await this.enviarComprobante(comprobante);
    
    if (respuestaRecepcion.estado !== 'RECIBIDA') {
      return {
        claveAcceso,
        estado: 'ERROR',
        mensajes: respuestaRecepcion.mensajes
      };
    }

    // 4. Esperar y consultar autorización (polling cada 3 segundos, máximo 10 intentos)
    for (let i = 0; i < 10; i++) {
      await new Promise(resolve => setTimeout(resolve, 3000));
      
      const respuestaAutorizacion = await this.consultarAutorizacion(claveAcceso);
      
      if (respuestaAutorizacion.estado === 'AUTORIZADO') {
        return {
          claveAcceso,
          numeroAutorizacion: respuestaAutorizacion.autorizaciones?.autorizacion.numeroAutorizacion,
          estado: 'AUTORIZADO'
        };
      } else if (respuestaAutorizacion.estado === 'NO AUTORIZADO') {
        return {
          claveAcceso,
          estado: 'NO AUTORIZADO',
          mensajes: respuestaAutorizacion.mensajes
        };
      }
    }

    // Timeout
    return {
      claveAcceso,
      estado: 'PENDIENTE',
      mensajes: [{ identificador: 'TIMEOUT', mensaje: 'La autorización está tardando más de lo esperado', informacionAdicional: '', tipo: 'ADVERTENCIA' }]
    };
  }

  private mapearFormaPago(formaPago: string): '01' | '16' | '19' | '20' {
    const mapeo: Record<string, '01' | '16' | '19' | '20'> = {
      'efectivo': '01',
      'tarjeta': '19',
      'transferencia': '20'
    };
    return mapeo[formaPago] || '01';
  }
}
