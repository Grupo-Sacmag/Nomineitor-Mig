namespace Nomina_2026_NET8
{
    /// <summary>
    /// Réplica de Sub reng() del VB6 utilizado inmediatamente
    /// antes de MdAbr_1 para generar las plantillas CFDI.
    ///
    /// N_ormal VB6:
    ///     0 = nómina ordinaria
    ///     1 = nómina extraordinaria/especial
    /// </summary>
    public static class CalculoRengVB6
    {
        public static void Calcular(
            DatosCFDIEmpleado e,
            bool esExtraordinaria,
            decimal salarioMinimo)
        {
            // ---------------------------------------------------------
            // Equivalentes VB6
            // ---------------------------------------------------------
            decimal sue3 = 0m;
            decimal via7 = 0m;
            decimal pva8 = 0m;
            decimal otr9 = 0m;
            decimal pee10 = 0m;

            decimal sub13 = 0m;
            decimal subc13 = e.SubsidioCausado;

            decimal agui5 = 0m;

            decimal isr12 = 0m;
            decimal ims14 = 0m;
            decimal pre15 = 0m;
            decimal fon16 = 0m;
            decimal pea17 = 0m;
            decimal ifv18 = 0m;

            decimal ptu1 = 0m;
            decimal ptu2 = 0m;
            decimal ptu3 = 0m;

            decimal tOi = 0m;

            decimal tPer = 0m;
            decimal tDed = 0m;
            decimal tNeto = 0m;

            decimal tGrav = 0m;
            decimal tExt = 0m;

            decimal tGravDed = 0m;
            decimal tExtDed = 0m;
            decimal tOded = 0m;

            // =========================================================
            // HÍBRIDO / SUBSIDIO
            // =========================================================

            // En ConstruirDatosCFDI e.Subsidio ya fue convertido
            // al signo que VB6 obtiene después del * -1.
            sub13 = e.Subsidio;

            // VB6:
            // Get 14, empleado, nom_com
            // subc13 = nom_com.subdio
            subc13 = e.SubsidioCausado;

            // =========================================================
            // PERCEPCIONES
            // =========================================================

            // VB6 columna 3
            sue3 = e.Salario;

            if (sue3 != 0m)
                tPer += sue3;

            // ---------------------------------------------------------
            // AGUINALDO
            // ---------------------------------------------------------
            if (esExtraordinaria)
            {
                if (e.TieneAguinaldoOriginal)
                {
                    agui5 = e.Aguinaldo;
                    tPer += agui5;
                }
                else
                {
                    agui5 = 0m;
                }
            }

            // ---------------------------------------------------------
            // OTRAS
            // ---------------------------------------------------------
            otr9 = e.Otras;

            if (otr9 != 0m)
                tPer += otr9;

            // ---------------------------------------------------------
            // VIÁTICOS / OF
            // ---------------------------------------------------------
            via7 = e.OF;

            if (via7 != 0m)
                tPer += via7;

            // ---------------------------------------------------------
            // PRIMA VACACIONAL
            // ---------------------------------------------------------
            pva8 = e.PrimaVacacional;

            // ---------------------------------------------------------
            // PERCEPCIÓN EXENTA
            // ---------------------------------------------------------
            pee10 = e.PercExenta;

            if (pva8 > 0m)
            {
                if (pee10 > 0m)
                {
                    tPer += pva8 + pee10;
                }
                else
                {
                    if (pva8 > salarioMinimo * 15m)
                    {
                        pee10 = salarioMinimo * 15m;
                        pva8 -= pee10;

                        tPer += pva8 + pee10;
                    }
                    else
                    {
                        // VB6:
                        // pee10 = pva8 - 0.01
                        // pva8 = 0.01
                        pee10 = pva8 - 0.01m;
                        pva8 = 0.01m;

                        tPer += pva8 + pee10;
                    }
                }
            }
            else
            {
                tPer += pee10;
            }

            // =========================================================
            // PTU - PARCHE 06/06/2017
            // =========================================================

            if (esExtraordinaria &&
                e.TienePTUOriginal)
            {
                // VB6:
                // ptu_1 = ConNom1(I7,6)
                ptu1 = e.PTU1;

                // VB6:
                // If IsNumeric(ConNom1(I7,10)) Then
                if (e.TieneExentoOriginal)
                {
                    ptu2 = e.ExentoOriginal;
                }
                else
                {
                    ptu2 = salarioMinimo * 15m;

                    if (ptu2 >= ptu1)
                    {
                        ptu2 = ptu1;
                        ptu1 = 0m;
                    }
                    else
                    {
                        ptu1 -= ptu2;
                    }
                }

                ptu3 = ptu1 + ptu2;

                // MUY IMPORTANTE:
                // VB6 sobrescribe t_per.
                // No lo suma.
                tPer = ptu3;

                pee10 = ptu2;
            }

            // =========================================================
            // TOTAL GRAVADO / EXENTO
            // =========================================================

            tGrav = tPer - pee10;
            tExt = pee10;

            // =========================================================
            // DEDUCCIONES
            // =========================================================

            // ---------------------------------------------------------
            // ISR
            // ---------------------------------------------------------
            if (e.TieneISROriginal)
            {
                isr12 = e.ISR;
                tDed += isr12;
            }
            else
            {
                // VB6:
                // If sub13 > 0 Then
                //   sub13 = sub13 - 0.01
                //   t_per = t_per + sub13
                //   isr12 = 0.01
                //   t_ded = t_ded + isr12
                // End If

                if (sub13 > 0m)
                {
                    sub13 -= 0.01m;
                    tPer += sub13;

                    isr12 = 0.01m;
                    tDed += isr12;
                }
            }

            // PARCHE VB6:
            //
            // If isr12 = 0 Then
            //     isr12 = 0.01
            //     t_ded = t_ded + isr12
            // End If
            if (isr12 == 0m)
            {
                isr12 = 0.01m;
                tDed += isr12;
            }

            // ---------------------------------------------------------
            // IMSS
            // ---------------------------------------------------------
            ims14 = e.IMSS;

            if (ims14 != 0m)
                tDed += ims14;

            // ---------------------------------------------------------
            // PRÉSTAMOS
            // ---------------------------------------------------------
            pre15 = e.Prestamos;

            if (pre15 != 0m)
                tDed += pre15;

            // ---------------------------------------------------------
            // FONACOT
            // ---------------------------------------------------------
            fon16 = e.FONACOT;

            if (fon16 != 0m)
                tDed += fon16;

            // ---------------------------------------------------------
            // PENSIÓN
            // ---------------------------------------------------------
            pea17 = e.PensionAlimenticia;

            if (pea17 != 0m)
                tDed += pea17;

            // ---------------------------------------------------------
            // INFONAVIT
            // ---------------------------------------------------------
            ifv18 = e.INFONAVIT;

            if (ifv18 != 0m)
                tDed += ifv18;

            // =========================================================
            // TOTALES FINALES
            // =========================================================

            tOded = tDed - isr12;
            tNeto = tPer - tDed;
            tExtDed = tDed - isr12;

            // =========================================================
            // DEVOLVER RESULTADO AL DTO
            // =========================================================

            e.Salario = sue3;
            e.OF = via7;
            e.PrimaVacacional = pva8;
            e.Otras = otr9;

            // Valor FINAL de pee10 después de todos los parches
            e.PercExenta = pee10;
            e.Exento = tExt;

            e.Aguinaldo = agui5;

            e.PTU1 = ptu1;
            e.PTU2 = ptu2;
            e.PTU3 = ptu3;

            e.Subsidio = sub13;
            e.SubsidioCausado = subc13;

            e.ISR = isr12;
            e.IMSS = ims14;
            e.Prestamos = pre15;
            e.FONACOT = fon16;
            e.PensionAlimenticia = pea17;
            e.INFONAVIT = ifv18;

            e.TotalIngresos = tPer;
            e.TotalDeducciones = tDed;
            e.Neto = tNeto;

            e.Gravado = tGrav;
            e.OtrasDeducciones = tOded;
            e.OtrosIngresosTotal = tOi;

            // Propiedades anteriores de compatibilidad
            e.AguinaldoGravado = agui5;

            e.PTUGravado = ptu1;
            e.PTUExento = ptu2;
        }
    }
}