import './globals.css'
import { Fraunces, Space_Grotesk } from 'next/font/google'

const heading = Fraunces({
  subsets: ['latin'],
  weight: ['400', '600', '700'],
  variable: '--font-heading',
})

const body = Space_Grotesk({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-body',
})

export const metadata = {
  title: 'Ori-Chara Chronicle',
  description:
    'An archival infrastructure for creative activities, integrating multi-layered identities and contexts.',
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className={`${heading.variable} ${body.variable}`}>
      <body>{children}</body>
    </html>
  )
}
