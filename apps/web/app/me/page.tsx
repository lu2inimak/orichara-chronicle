'use client'

import { useEffect, useMemo, useState } from 'react'

const STORAGE_KEY = 'occ_username'

type MeResponse = {
  user?: { id?: string }
  owned_character_ids?: string[]
  hosted_world_ids?: string[]
  affiliation_ids?: string[]
}

export default function MyPage() {
  const [username, setUsername] = useState('')
  const [saved, setSaved] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [ownedCharacters, setOwnedCharacters] = useState<string[]>([])
  const [hostedWorlds, setHostedWorlds] = useState<string[]>([])
  const [affiliations, setAffiliations] = useState<string[]>([])

  const apiBaseUrl = useMemo(
    () => process.env.NEXT_PUBLIC_API_BASE_URL ?? '',
    []
  )

  useEffect(() => {
    const stored = window.localStorage.getItem(STORAGE_KEY) ?? ''
    setUsername(stored)
  }, [])

  useEffect(() => {
    if (!apiBaseUrl) {
      setError('API base URL is not set.')
      return
    }
    const token = window.localStorage.getItem(STORAGE_KEY) ?? ''
    if (!token) {
      setError('Sign in to load your data.')
      return
    }
    setLoading(true)
    setError(null)
    fetch(`${apiBaseUrl.replace(/\/$/, '')}/me`, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Failed: ${response.status}`)
        }
        const json = (await response.json()) as { data?: MeResponse }
        const data = json.data ?? {}
        setOwnedCharacters(data.owned_character_ids ?? [])
        setHostedWorlds(data.hosted_world_ids ?? [])
        setAffiliations(data.affiliation_ids ?? [])
      })
      .catch((err) => {
        setError(err instanceof Error ? err.message : 'Failed to load.')
      })
      .finally(() => setLoading(false))
  }, [apiBaseUrl])

  const handleSave = () => {
    const normalized = username.trim()
    if (normalized.length === 0) return
    window.localStorage.setItem(STORAGE_KEY, normalized)
    setSaved(true)
    window.setTimeout(() => setSaved(false), 1200)
  }

  return (
    <main className="profile-page">
      <div className="profile-header">
        <div>
          <h1>My Page</h1>
          <p>Owned assets and affiliations.</p>
        </div>
        <div className="profile-actions">
          <a className="btn secondary" href="/create/character">
            Create Character
          </a>
          <a className="btn secondary" href="/create/world">
            Create World
          </a>
          <a className="btn secondary" href="/post">
            New Post
          </a>
        </div>
      </div>

      <section className="profile-section">
        <div className="profile-section-header">
          <h2>Account</h2>
        </div>
        <div className="profile-card">
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
          <button className="btn primary" type="button" onClick={handleSave}>
            Save
          </button>
          {saved ? <span className="auth-note">Saved</span> : null}
        </div>
      </section>

      <section className="profile-section">
        <div className="profile-section-header">
          <h2>Owned Characters</h2>
          <a className="btn secondary" href="/create/character">
            Create
          </a>
        </div>
        <div className="profile-list">
          {loading ? (
            <span className="auth-note">Loading...</span>
          ) : error ? (
            <span className="auth-note">{error}</span>
          ) : ownedCharacters.length === 0 ? (
            <span className="auth-note">No characters yet.</span>
          ) : (
            ownedCharacters.map((id) => <div key={id}>{id}</div>)
          )}
        </div>
      </section>

      <section className="profile-section">
        <div className="profile-section-header">
          <h2>Owned Worlds</h2>
          <a className="btn secondary" href="/create/world">
            Create
          </a>
        </div>
        <div className="profile-list">
          {loading ? (
            <span className="auth-note">Loading...</span>
          ) : error ? (
            <span className="auth-note">{error}</span>
          ) : hostedWorlds.length === 0 ? (
            <span className="auth-note">No worlds yet.</span>
          ) : (
            hostedWorlds.map((id) => <div key={id}>{id}</div>)
          )}
        </div>
      </section>

      <section className="profile-section">
        <div className="profile-section-header">
          <h2>Affiliations</h2>
          <a className="btn secondary" href="/create/character">
            Create
          </a>
        </div>
        <div className="profile-list">
          {loading ? (
            <span className="auth-note">Loading...</span>
          ) : error ? (
            <span className="auth-note">{error}</span>
          ) : affiliations.length === 0 ? (
            <span className="auth-note">No affiliations yet.</span>
          ) : (
            affiliations.map((id) => <div key={id}>{id}</div>)
          )}
        </div>
      </section>
    </main>
  )
}
