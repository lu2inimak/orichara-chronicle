'use client'

import { useRef, useState } from 'react'

export default function CreateWorldPage() {
  const [name, setName] = useState('')
  const [summary, setSummary] = useState('')
  const [coverUrl, setCoverUrl] = useState('')
  const [fileName, setFileName] = useState<string | null>(null)
  const [status, setStatus] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)
  const fileRef = useRef<HTMLInputElement | null>(null)

  const apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? ''

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (!apiBaseUrl) {
      setStatus('API base URL is not set.')
      return
    }
    const normalizedName = name.trim()
    if (!normalizedName) {
      setStatus('Name is required.')
      return
    }
    setSubmitting(true)
    setStatus(null)
    try {
      const username = window.localStorage.getItem('occ_username') ?? ''
      const response = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/worlds`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: username ? `Bearer ${username}` : '',
        },
        body: JSON.stringify({
          name: normalizedName,
          description: summary.trim() || null,
        }),
      })
      if (!response.ok) {
        setStatus(`Failed: ${response.status}`)
        return
      }
      setStatus('Created.')
      setName('')
      setSummary('')
      setCoverUrl('')
    } catch {
      setStatus('Failed to create.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleUpload = async () => {
    const file = fileRef.current?.files?.[0]
    if (!file) {
      setStatus('Select an image first.')
      return
    }
    if (!apiBaseUrl) {
      setStatus('API base URL is not set.')
      return
    }
    setStatus('Uploading...')
    try {
      const presign = await fetch(`${apiBaseUrl.replace(/\/$/, '')}/uploads/presign`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          file_name: file.name,
          content_type: file.type,
          prefix: 'covers',
        }),
      })
      if (!presign.ok) {
        setStatus(`Presign failed: ${presign.status}`)
        return
      }
      const payload = (await presign.json()) as { data?: { upload_url?: string; public_url?: string } }
      const uploadUrl = payload.data?.upload_url ?? ''
      const publicUrl = payload.data?.public_url ?? ''
      if (!uploadUrl) {
        setStatus('Upload URL missing.')
        return
      }
      const response = await fetch(uploadUrl, {
        method: 'PUT',
        headers: {
          'Content-Type': file.type || 'application/octet-stream',
        },
        body: file,
      })
      if (!response.ok) {
        setStatus(`Upload failed: ${response.status}`)
        return
      }
      if (publicUrl) {
        setCoverUrl(publicUrl)
      }
      setFileName(file.name)
      setStatus('Uploaded.')
    } catch {
      setStatus('Upload failed.')
    }
  }

  return (
    <main className="auth-page">
      <div className="auth-card">
        <h1>Create World</h1>
        <p>Create a new world.</p>
        <form onSubmit={handleSubmit}>
          <label>
            Cover File
            <input ref={fileRef} name="cover_file" type="file" accept="image/*" />
          </label>
          <button className="btn secondary" type="button" onClick={handleUpload}>
            Upload to S3
          </button>
          {fileName ? <span className="auth-note">Uploaded: {fileName}</span> : null}
          <label>
            Name
            <input
              name="name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              placeholder="World name"
            />
          </label>
          <label>
            Summary
            <textarea
              name="summary"
              value={summary}
              onChange={(event) => setSummary(event.target.value)}
              placeholder="Short summary"
              rows={4}
            />
          </label>
          <label>
            Cover URL
            <input
              name="cover_url"
              value={coverUrl}
              onChange={(event) => setCoverUrl(event.target.value)}
              placeholder="https://"
            />
          </label>
          <button className="btn primary" type="submit">
            {submitting ? 'Creating...' : 'Create'}
          </button>
          {status ? <span className="auth-note">{status}</span> : null}
        </form>
      </div>
    </main>
  )
}
