# Adaptación de Nómina C# a RUEP V9

Esta entrega contiene los **57 archivos originales** del ZIP compartido por el usuario, con cambios en los modelos, servicios y los formularios prioritarios. Los archivos `.Designer.cs`, `.resx` y las demás clases que no requirieron ajustes se conservaron.

> **IMPORTANTE:** En el ZIP original no hay `.csproj`, `.sln`, propiedades Settings generadas ni archivos de prueba RUEP V9 cifrados. Por ello **no se compiló ni ejecutó el proyecto completo** en esta entrega. Sustituir estos `.cs` en una rama de pruebas, compilar en Visual Studio/.NET 8 y probar sobre copias de datos, NUNCA sobre la nómina productiva sin verificar.

## Contrato JSON V9

- Raíz: `idEmpresa`, `nombre`, `anio_fiscal`, `uma_por_dia`, `salario_minimo`, `fecha_actualizacion`, `modificado_por`, `datos_fiscales`, `cfdi_nomina`, `personal`.
- `datos_fiscales`: `rfc_empresa`, `direccion_empresa`, `registro_patronal_empresa`, `regimen_fiscal`, `representante_legal_empresa` (con `rfc_representante_legal` y `curp_representante_legal`) y `banamex`.
- `cfdi_nomina`: `folio`, `serie`, `consecutivo` y `riesgo_imss` **a nivel empresa**. El registro patronal vive exclusivamente en `datos_fiscales.registro_patronal_empresa`.
- Empleado: `idEmpleado`, nombre, apellidos, `rfc`, `nss_imss`, `fecha_alta`, `fecha_baja`, `ingresos`, `cfdi`, `datos_personales`, `historial_laboral`, auditoría. `ingresos.integrado` se persiste y `ingresos.isr` no existe.
- `datos_personales.numero_tarjeta_banamex` corresponde a Bnxcla.dno, **no a una CLABE**.
- `historial_laboral[]` conserva `idPeriodo`, `tipo_movimiento`, `fecha_alta`, `fecha_baja`, `id_empresa_origen`, `id_empleado_origen` y `respeta_antiguedad`.

## Modificaciones

| Archivo | Cambio |
|---|---|
| `RuepRoot.cs` | Clases del esquema V9; historial laboral; integrado dentro de `ingresos`; extensiones nulas omitidas; preservación de propiedades desconocidas para evitar pérdida incidental |
| `RuepService.cs` | Lectura V9 y asignación a `EmpleadoCompleto`; sin guardado automático en la carga; guardado de empleados por ID **sobre el objeto original** para no borrar historia ni campos no editados; registro patronal junto a datos fiscales |
| `EmpleadoCompleto.cs` | Propiedad `TipoVialidad` para no perder `cfdi.tipo_vialidad` al editar y guardar |
| `FormPrincipal.cs` | `anio_fiscal`, requisitos de DNO V9, importación SQL opcional al generar; mensaje sin bloquear cuando falla la consulta SQL |
| `FormEDICIONCENTRALPERSONAL.cs` | `anio_fiscal`, SDI inicial leído del archivo; riesgo IMSS mostrado desde la configuración de empresa como solo lectura |
| `FormCaptura.cs` | Año fiscal y registro patronal desde datos fiscales; captura CFDI con origen V9; Banamex se alimenta del dato correcto del DTO |
| `FormEmpresaUnificado.cs` | Edición/carga de empresa con rutas fiscales exactas; inicialización defensiva de subobjetos fiscales |
| `DatosInicioCfdiNomina.cs` | El valor inicial de registro patronal proviene de `datos_fiscales`, no de la sección CFDI |
| `RuepGenerator.cs` | Generador propio C# reescrito para V9: lee `EMP_CFDI.DNO`; Bnxcla opcional; precios Currency con 4 decimales; periodos MIGRACION_VB6; posiciones físicas de empleados; no reconstruye campos de `maestro.dno` ajenos a V9; nunca sobrescribe RUEP existente |
| `BDSyncService.cs` | Prohibidas las escrituras SQL (métodos públicos lanzan excepción). SOLO SELECT; complementa campos vacíos; no cruza IDs ocupados y retiene tipo de vialidad. Clave de consulta SQL en memoria |
| `CryptoService.cs` | Guardado temporal/renombrado y respaldo timestamp para `RUEP.dat`; preserva cifrado IV(16)+AES-CBC/PKCS7 y clave Settings existente |

## Consideraciones

1. `clave_empresa` es auxiliar para SQL y **no forma parte del JSON V9**. El servicio la resuelve por nombre durante la sincronización, sin guardarla. Las empresas no reconocibles necesitan una regla de correspondencia, no metadatos dentro de V9.
2. Hay campos adicionales de los formularios que **no aparecen en el V9 mínimo migrado**: `departamento`, `puesto`, `forma_pago_sat`, `tipo_contrato`, `tipo_jornada`, `obras`; si se capturan con valores, se conservan como atributos opcionales. También `estado_domicilio`, `estado_cont_domicilio`, `link_sat` y `nombre_vialidad` permanecen opcionales. Si la meta es un esquema estrictamente cerrado sin extensiones, habrá que decidir dónde persistirlos ANTES de habilitar esos controles para edición, para no perder datos.
3. `riesgo_imss` empresarial se edita en el flujo de CFDI de `FormCaptura`, no en el expediente individual.
4. La fecha/antigüedad en reingresos se interpreta desde la última entrada de tipo `REINGRESO` de `historial_laboral`; al guardar altas/bajas desde el formulario se mantiene el periodo original y se agrega un reingreso cuando corresponde.
5. El ISR no se serializa en `ingresos`. Sigue calculándose en ejecución.
6. Las escrituras a SQL están inhabilitadas. SQL solamente puede consultar, y los `.dno` originales no se modifican.
7. El generador en C# no está pensado para sustituir los respaldos completos de migración: genera V9 con los DNO básicos, tal como lo hace el migrador V9 acordado.

## Pasos de integración y pruebas

1. Crear una rama Git (por ejemplo `ruep-v9-integracion`) y realizar copia de seguridad de código, `RUEP.dat` y `.dno` anonimizados.
2. Copiar el contenido de este ZIP a los archivos correspondientes de la solución actual en Visual Studio. No reemplazar archivos de Settings ni claves AES.
3. Compilar con .NET 8 y las dependencias del proyecto original. Confirmar que no hay referencias a `ano_fiscal`, `clabe_banamex`, `imss` (modelo de empleado) ni `datos_fiscales.registro_patronal` antiguos.
4. Con una copia de RUEP V9 y la misma clave AES configurada: abrir FormPrincipal y verificar razón social, año, UMA y salario mínimo; cerrar sin editar y comprobar que el RUEP **no se reescribe**.
5. Abrir FormEmpresaUnificado: comprobar RFC, domicilio, registro patronal y representante legal. Editar un dato pequeño y verificar que se guarde en la ruta V9 correcta y permanezca `cfdi_nomina`.
6. Abrir FormEDICIONCENTRALPERSONAL: validar NSS, 16 dígitos de tarjeta, correo, domicilio, campos extendidos e `ingresos.integrado` sin recalculado al entrar. Editar un único dato y verificar que todas las entradas de `historial_laboral` y sus IDs sigan presentes.
7. Probar reingreso de un empleado **de prueba**: agregar un nuevo periodo a `historial_laboral` sin borrar `MIGRACION_VB6` ni cambiar `idEmpleado`.
8. Abrir FormCaptura: validar salario base e integrado, BANAMEX y registro patronal en CFDI, generar nómina normal y especial con datos de prueba.
9. Comprobar que una SQL caída no impide generar el RUEP desde DNO, que SQL solo haga SELECT y que no se sobresciban campos locales.
10. Antes/después: usar la opción 2 de `MigradorRUEP_V9.py` para descifrar copias y comparar el JSON. Cada guardado debería crear una copia en `Respaldos_RUEP`.

## Entorno de compilación

El ZIP de entrada contiene fuentes `.cs` y recursos, **pero no** contiene `.csproj` ni `.sln`. No se pudo ejecutar una compilación real aquí; la comprobación automatizada realizada fue estructural y de referencias textuales. Para una prueba de compilación concluyente debe integrarse en la solución de Visual Studio correspondiente.
