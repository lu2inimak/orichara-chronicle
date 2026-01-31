'use client'

import { useRouter } from 'next/navigation'
import { useState } from 'react'

const STORAGE_KEY = 'occ_username'

export default function LoginPage() {
  const router = useRouter()
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  const handleSubmit = (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalized = username.trim()
    if (normalized.length === 0) {
      return
    }
    window.localStorage.setItem(STORAGE_KEY, normalized)
    router.push('/')
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>Sign In</h1>
        <p>Any password is accepted. Use this to switch usernames.</p>
        <form onSubmit={handleSubmit}>
          <label>
            Username
            <input
              name="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              placeholder="your name"
              required
            />
          </label>
          <label>
            Password
            <input
              name="password"
              type="password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="anything"
            />
          </label>
          <button className="btn primary" type="submit">
            Sign In
          </button>
        </form>
      </div>
    </main>
  )
}
