// Logotipo aprobado: la Q como lupa (anillo con mango diagonal) y "qualia" en minúsculas.
// "claro" para fondos carbón (sidebar, panel de marca); "oscuro" para superficies claras.

interface PropsLogo {
  tono?: 'claro' | 'oscuro'
  tamano?: 'sm' | 'md' | 'lg'
}

const ALTURAS = { sm: 'h-7', md: 'h-8', lg: 'h-12' } as const

export function Logo({ tono = 'oscuro', tamano = 'md' }: PropsLogo) {
  const color = tono === 'claro' ? '#F8F7F4' : '#18181B'
  return (
    <svg
      viewBox="0 0 150 40"
      className={`${ALTURAS[tamano]} w-auto`}
      role="img"
      aria-label="Qualia"
    >
      <circle cx="18" cy="19" r="12" fill="none" stroke={color} strokeWidth="5.5" />
      <path d="M26 27.5 33 34.5" stroke={color} strokeWidth="5.5" strokeLinecap="round" />
      <text
        x="44"
        y="29"
        fill={color}
        fontFamily="Manrope, sans-serif"
        fontWeight="700"
        fontSize="29"
        letterSpacing="-0.5"
      >
        qualia
      </text>
    </svg>
  )
}
