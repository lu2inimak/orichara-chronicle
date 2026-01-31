'use client'

import Link from 'next/link'
import { useEffect, useState } from 'react'

const STORAGE_KEY = 'occ_username'

export default function TopRightAuth() {
  const [username, setUsername] = useState<string | null>(null)

  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY)
    setUsername(stored && stored.trim().length > 0 ? stored : null)
  }, [])

  return (
    <div className="auth-float">
      {username ? (
        <>
          <span className="auth-name">{username}</span>
          <Link className="btn primary" href="/me">
            My Page
          </Link>
        </>
      ) : (
        <Link className="btn primary" href="/login">
          Sign In
        </Link>
      )}
    </div>
  )
}
