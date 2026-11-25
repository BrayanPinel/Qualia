import axios from 'axios'
import { borrarSesion, leerSesion } from './almacenSesion'

// Instancia única de Axios para toda la app. baseURL termina en /api (ver example.env).
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

// Rutas donde un 401 significa "credenciales incorrectas" y no "sesión vencida":
// ahí el error se muestra en el formulario y no se cierra ninguna sesión.
const RUTAS_LOGIN = ['/auth/login', '/plataforma/auth/login']

// Petición: adjunta el JWT de la sesión como Bearer.
apiClient.interceptors.request.use((config) => {
  const sesion = leerSesion()
  if (sesion) {
    config.headers.Authorization = `Bearer ${sesion.accessToken}`
  }
  return config
})

// Respuesta: ante un 401 se borra la sesión. RutaProtegida detecta el cambio y lleva a /login.
apiClient.interceptors.response.use(
  (respuesta) => respuesta,
  (error) => {
    const esLogin = RUTAS_LOGIN.some((ruta) => error.config?.url?.endsWith(ruta))
    if (axios.isAxiosError(error) && error.response?.status === 401 && !esLogin) {
      borrarSesion()
    }
    return Promise.reject(error)
  },
)
