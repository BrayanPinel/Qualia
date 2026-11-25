import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { LoginPlataformaView } from './features/auth/components/LoginPlataformaView'
import { LoginView } from './features/auth/components/LoginView'
import { RedireccionInicial } from './features/auth/components/RedireccionInicial'
import { RutaProtegida } from './features/auth/components/RutaProtegida'
import { SesionProvider } from './features/auth/context/SesionProvider'
import { RegistroEmpresaView } from './features/empresas/components/RegistroEmpresaView'
import { InicioView } from './features/inicio/components/InicioView'
import { EmpresasPlataformaView } from './features/plataforma/components/EmpresasPlataformaView'
import { UsuariosView } from './features/usuarios/components/UsuariosView'
import { AppLayout } from './layouts/AppLayout'
import { ModuloPendienteView, NoEncontradaView } from './shared/components/VistasGenerales'

// Orquestador raíz: solo declara las rutas. Cada módulo entrega aquí sus vistas por PR
// (las marcadas con ModuloPendienteView se reemplazan por la vista real del integrante).

const EMPRESA = ['AdminEmpresa', 'Supervisor', 'Agente', 'Auditor'] as const

function App() {
  return (
    <SesionProvider>
      <BrowserRouter>
        <Routes>
          {/* Públicas */}
          <Route path="/" element={<RedireccionInicial />} />
          <Route path="/login" element={<LoginView />} />
          <Route path="/plataforma/login" element={<LoginPlataformaView />} />
          <Route path="/registro" element={<RegistroEmpresaView />} />

          {/* Protegidas: requieren JWT; cada grupo exige además su rol */}
          <Route element={<RutaProtegida />}>
            <Route element={<AppLayout />}>
              <Route element={<RutaProtegida roles={EMPRESA} />}>
                <Route path="/inicio" element={<InicioView />} />
              </Route>

              <Route element={<RutaProtegida roles={['AdminEmpresa']} />}>
                <Route path="/usuarios" element={<UsuariosView />} />
                <Route path="/campanas/*" element={<ModuloPendienteView titulo="Campañas y rúbricas" />} />
                <Route path="/suscripcion" element={<ModuloPendienteView titulo="Suscripción" />} />
              </Route>

              <Route element={<RutaProtegida roles={['AdminEmpresa', 'Supervisor', 'Agente']} />}>
                <Route path="/llamadas/*" element={<ModuloPendienteView titulo="Llamadas" />} />
                <Route path="/desempeno/*" element={<ModuloPendienteView titulo="Desempeño" />} />
              </Route>

              <Route element={<RutaProtegida roles={['Supervisor']} />}>
                <Route path="/revision/*" element={<ModuloPendienteView titulo="Revisión de calidad" />} />
              </Route>

              <Route element={<RutaProtegida roles={['AdminEmpresa', 'Supervisor']} />}>
                <Route path="/disputas/*" element={<ModuloPendienteView titulo="Disputas" />} />
              </Route>

              <Route element={<RutaProtegida roles={['AdminEmpresa', 'Supervisor', 'Auditor']} />}>
                <Route path="/reportes/*" element={<ModuloPendienteView titulo="Reportes" />} />
              </Route>

              <Route element={<RutaProtegida roles={['PlatformAdmin']} />}>
                <Route path="/plataforma/empresas" element={<EmpresasPlataformaView />} />
              </Route>
            </Route>
          </Route>

          <Route path="*" element={<NoEncontradaView />} />
        </Routes>
      </BrowserRouter>
    </SesionProvider>
  )
}

export default App
